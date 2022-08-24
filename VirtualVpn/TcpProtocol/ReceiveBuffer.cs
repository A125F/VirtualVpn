﻿namespace VirtualVpn.TcpProtocol;

public class ReceiveBuffer
{
    private readonly object _lock = new();
    private readonly List<TcpSegment> _segments = new();
    
    /// <summary> Location that next read-out should start from </summary>
    private long _readHead = long.MaxValue;
    
    /// <summary> Flag set when reading starts. New packets before the start point are rejected if true </summary>
    private bool _isReading;

    public ReceiveBuffer()
    {
        _isReading = false;
        IsComplete = false;
    }

    public void Insert(TcpSegment seg)
    {
        lock (_lock)
        {
            if (_isReading && seg.SequenceNumber < _readHead)
            {
                Log.Warn($"Segment at sequence {seg.SequenceNumber} ignored because reading has already passed that point");
                return;
            }

            _segments.Add(seg);
            _segments.Sort((a, b) => a.SequenceNumber.CompareTo(b.SequenceNumber));
            _readHead = _segments[0].SequenceNumber;
        }
    }

    /// <summary>
    /// Gets the next sequence number after the longest contiguous sequence of bytes
    /// stored after the initial value given.
    /// </summary>
    /// <param name="initial">initial sequence number to count from (next byte expected)</param>
    /// <returns>the next byte after the highest contiguous sequence number from 'initial' that is held</returns>
    public uint ContiguousSequence(long initial) // lib/tcp/tcp.c:361
    {
        lock (_lock)
        {
            _segments.Sort((a, b) => a.SequenceNumber.CompareTo(b.SequenceNumber));
            var position = initial;

            foreach (var segment in _segments)
            {
                var segSeq = segment.SequenceNumber;
                var segLen = segment.Payload.Length;
                var segEnd = segSeq + segLen - 1;

                if (SeqGtEq(position, segSeq) && SeqLtEq(position, segEnd)) // position is inside the segment
                {
                    position = segEnd + 1; // next byte after segment
                }
                else if (SeqGt(position, segEnd)) // duplicate
                {
                    // continue
                }
                else break;
            }

            return (uint)position;
        }
    }

    public long EntireSize => _segments.Sum(s => s.Payload.Length);
    
    private static bool SeqGtEq(long a, long b) => (a - b) >= 0;
    private static bool SeqLtEq(long a, long b) => (a - b) <= 0;
    private static bool SeqGt(long a, long b) => (a - b) > 0;

    public void SetComplete()
    {
        lock (_lock)
        {
            IsComplete = true;
        }
    }

    public bool IsComplete { get; set; }

    /// <summary>
    /// Read data into supplied buffer.
    /// The next read will continue from where previous left off.
    /// Buffered segments will be removed when all their data is read.
    /// Once a read has started, no new segments will be accepted if they
    /// are before the current read-point.
    /// If data is non-contiguous, reading will stop at the gap.
    /// </summary>
    public int ReadOutAndUpdate(byte[] buffer, int offset, int length)
    {
        if (offset >= buffer.Length) throw new Exception("Offset greater than buffer size");
        if (length < 1) return 0;
        var total = 0;
        lock (_lock)
        {
            _isReading = true;
            
            if (_segments.Count < 1) return 0;
            
            var endOfData = ContiguousSequence(_readHead);
            var available = endOfData - _readHead;
            
            var end = Min((int)available, buffer.Length - offset, length);
            var idx = offset;

            // read data out of segments
            foreach (var segment in _segments)
            {
                var segStart = segment.SequenceNumber;
                var segData = segment.Payload;
                var segEnd = segStart + segData.Length;

                if (_readHead >= segEnd) continue;
                var segOffset = _readHead - segStart;
                if (segOffset < 0) throw new Exception($"Unexpected hole in data. Expected segment starting at or before {_readHead}, but it was {segStart}");
                if (segOffset >= segData.Length) throw new Exception($"Unexpected segment length. Expected it to end after {segOffset}, but it ends at {segData.Length}");

                for (var i = segOffset; i < segData.Length; i++)
                {
                    buffer[idx++] = segData[i];
                    total++;
                    if (idx > end) break;
                }
                if (idx > end) break;
            }
    
            // remove any segments that are used up
            while (FirstSegmentIsUsedUp())
            {
                var seg = _segments[0];
                Log.Debug($"Segment used up: {seg.SequenceNumber}..{(seg.SequenceNumber + seg.Payload.Length - 1)}; read head is at {_readHead}");
                _segments.RemoveAt(0);
            }
        }
        return total;
    }

    private bool FirstSegmentIsUsedUp()
    {
        if (_segments.Count < 1) return false;
        var seg = _segments[0];
        var end = seg.SequenceNumber + seg.Payload.Length;
        return _readHead >= end;
    }

    public static int Min(int a, int b, int c)
    {
        var x = a > b ? b : a;
        var y = b > c ? c : b;
        return x > y ? y : x;
    }
}