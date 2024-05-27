﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BookingAds.Common.Models.Base;
using BookingAds.Entities;

namespace BookingAds.Areas.Admin.Models.ManageOrder
{
    public class ViewManageOrder : ViewPaginateOutputBase<Order>
    {
        public int Status { get; set; }

        public string FromDatetime { get; set; }

        public string ToDatetime { get; set; }

        public int SearchField { get; set; }

        public string SearchValue { get; set; }
    }
}