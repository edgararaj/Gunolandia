using System;
using System.Runtime.InteropServices;

namespace Gunolandia
{
    [StructLayout(LayoutKind.Sequential)]
    public struct UNSIGNED_RATIO
    {
        public UInt32 uiNumerator;
        public UInt32 uiDenominator;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DWM_TIMING_INFO
    {
        public UInt32 cbSize;
        public UNSIGNED_RATIO rateRefresh;
        public UInt64 qpcRefreshPeriod;
        public UNSIGNED_RATIO rateCompose;
        public UInt64 qpcVBlank;
        public UInt64 cRefresh;
        public UInt32 cDXRefresh;
        public UInt64 qpcCompose;
        public UInt64 cFrame;
        public UInt32 cDXPresent;
        public UInt64 cRefreshFrame;
        public UInt64 cFrameSubmitted;
        public UInt32 cDXPresentSubmitted;
        public UInt64 cFrameConfirmed;
        public UInt32 cDXPresentConfirmed;
        public UInt64 cRefreshConfirmed;
        public UInt32 cDXRefreshConfirmed;
        public UInt64 cFramesLate;
        public UInt32 cFramesOutstanding;
        public UInt64 cFrameDisplayed;
        public UInt64 qpcFrameDisplayed;
        public UInt64 cRefreshFrameDisplayed;
        public UInt64 cFrameComplete;
        public UInt64 qpcFrameComplete;
        public UInt64 cFramePending;
        public UInt64 qpcFramePending;
        public UInt64 cFramesDisplayed;
        public UInt64 cFramesComplete;
        public UInt64 cFramesPending;
        public UInt64 cFramesAvailable;
        public UInt64 cFramesDropped;
        public UInt64 cFramesMissed;
        public UInt64 cRefreshNextDisplayed;
        public UInt64 cRefreshNextPresented;
        public UInt64 cRefreshesDisplayed;
        public UInt64 cRefreshesPresented;
        public UInt64 cRefreshStarted;
        public UInt64 cPixelsReceived;
        public UInt64 cPixelsDrawn;
        public UInt64 cBuffersEmpty;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TimeCaps
    {
        public UInt32 wPeriodMin;
        public UInt32 wPeriodMax;
    };
}
