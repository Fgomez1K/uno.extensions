﻿using System;

namespace Uno.Extensions.Navigation
{
    public interface INavigator
    {
        void Navigate(Type destinationPage, object viewModel = null);

        void GoBack();

        void Clear();
    }
}
