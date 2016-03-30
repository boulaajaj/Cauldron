﻿using System;

namespace Couldron.Controls
{
    public sealed class NavigationFrameBackRequestedEventArgs : EventArgs
    {
        internal NavigationFrameBackRequestedEventArgs()
        {
            this.IsHandled = false;
        }

        public bool IsHandled { get; set; }
    }
}