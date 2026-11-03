// Created by SwanDEV 2019

using System;
using System.Collections.Generic;
using System.Threading;
using ThreadPriority = System.Threading.ThreadPriority;

internal sealed class ProGifWorker
{
    private static int workerId = 1;

    private Thread thread;
    private int id;

    internal List<Frame> m_Frames;
    internal ProGifEncoder m_Encoder;
    internal Action<int> m_OnFileSaveProgress;

    internal ProGifWorker(ThreadPriority priority)
    {
        id = workerId++;
        if (workerId == int.MaxValue) workerId = 1;
        thread = new Thread(Run);
        thread.Priority = priority;
    }

    internal void Start()
    {
        thread.Start();
    }

    void Run()
    {
        m_Encoder.Start();
        for (int i = 0; i < m_Frames.Count; i++)
        {
            m_Encoder.AddFrame(m_Frames[i]);
            if (m_OnFileSaveProgress != null) m_OnFileSaveProgress(id);
        }
        m_Encoder.Finish();
    }
}
