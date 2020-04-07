﻿using System.Collections.Generic;

namespace GenderPayGap.WebUI.Views.Components.PaginationLinks
{
    public class PaginationPages
    {
        public List<PaginationPage> Pages { get; set; }
        public int CurrentPageIndex { get; set; }
    }

    public class PaginationPage
    {
        public string Title { get; set; }
        public string Url { get; set; }
    }
}