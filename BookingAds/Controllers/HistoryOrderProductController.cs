﻿using System.Web.Mvc;
using System.Web.Security;
using BookingAds.Areas.Admin.Repository.Product;
using BookingAds.Areas.Admin.Repository.Product.Abstractions;
using BookingAds.Areas.Admin.Repository.Order;
using BookingAds.Areas.Admin.Repository.Order.Abstractions;
using BookingAds.Attributes.Filters;
using BookingAds.Common.Repository.Account;
using BookingAds.Common.Repository.Account.Abstractions;
using BookingAds.Constants;
using BookingAds.Entities;
using BookingAds.Models.HistoryOrderProduct;
using BookingAds.Modules;
using BookingAds.Repository.HistoryOrderProduct;
using BookingAds.Repository.HistoryOrderProduct.Abstractions;

namespace BookingAds.Controllers
{
    [RoleFilter(Roles = RoleConstant.EMPLOYEE)]
    public class HistoryOrderProductController : Controller
    {
        #region Repository
        private readonly IAccountRepository _accountRepo = new AccountRepository();
        private readonly IHistoryOrderProductRepository _historyOrderProductRepo = new HistoryOrderProductRepository();
        private readonly IOrderRepository _orderRepo = new OrderRepository();
        private readonly IProductRepository _productRepo = new ProductRepository();
        #endregion
        #region Constant
        private const int PAGE_SIZE = 10;
        private const string FormatDateTimeFilter = "yyyy-MM-ddTHH:mm";
        private const string ControllerName = "HistoryOrderProduct";
        private const string ActionIndex = "Index";
        private const string ActionSearchHistoryOrder = "SearchHistoryOrder";
        private const string ActionProducts = "Products";
        private const string ActionEdit = "Edit";
        private const string ActionCanceled = "Canceled";
        #endregion
        #region Action

        // GET: HistoryOrderProduct
        [HttpGet]
        [ActionName(ActionIndex)]
        public ActionResult Index()
        {
            ViewBag.Title = "Lịch sử đặt quảng cáo";
            var currentEmployee = ConvertUtils<Employee>.Deserialize(User.Identity.Name);

            var viewData = new ViewFilterHistory()
            {
                OrderStatus = OrderStatus.DEFAULT,
                SearchValue = string.Empty,
                Page = 1,
                PageSize = PAGE_SIZE,
                DateStart = string.Empty,
                DateEnd = string.Empty,
            };

            var model = new ViewIndex()
            {
                DateStart = viewData.DateStart,
                DateEnd = viewData.DateEnd,
                OrderStatus = viewData.OrderStatus,
                SearchValue = viewData.SearchValue,
                Data = _historyOrderProductRepo.LoadHistoryOrderProduct(viewData, currentEmployee.EmployeeID),
                CurrentPage = viewData.Page,
                CurrentPageSize = viewData.PageSize,
                TotalRow = _historyOrderProductRepo.Count(viewData, currentEmployee.EmployeeID),
            };

            return View(model);
        }

        // Post: HistoryOrderProduct/SearchHistoryOrder
        [HttpPost]
        [ActionName(ActionSearchHistoryOrder)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA3147:Mark Verb Handlers With Validate Antiforgery Token", Justification = "<Pending>")]
        public ActionResult SearchHistoryOrder(ViewFilterHistory viewData)
        {
            if (viewData == null)
            {
                return Json(ConstantsReturnMessengerOrder.ErrorNull, JsonRequestBehavior.AllowGet);
            }

            if (!DateTimeUtils.IsValidDateTimeSQL(viewData.DateStart)
               || !DateTimeUtils.IsValidDateTimeSQL(viewData.DateEnd))
            {
                return Json(ConstantsReturnMessengerOrder.InvalidDateTime, JsonRequestBehavior.AllowGet);
            }

            var currentEmployee = ConvertUtils<Employee>.Deserialize(User.Identity.Name);

            var model = new ViewIndex()
            {
                DateEnd = viewData.DateEnd,
                DateStart = viewData.DateStart,
                OrderStatus = viewData.OrderStatus,
                SearchValue = viewData.SearchValue,
                Data = _historyOrderProductRepo.LoadHistoryOrderProduct(viewData, currentEmployee.EmployeeID),
                CurrentPage = viewData.Page,
                CurrentPageSize = viewData.PageSize,
                TotalRow = _historyOrderProductRepo.Count(viewData, currentEmployee.EmployeeID),
            };

            return View(model);
        }

        // POST: HistoryOrderProduct/Products
        [HttpPost]
        [ActionName(ActionProducts)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA3147:Mark Verb Handlers With Validate Antiforgery Token", Justification = "<Pending>")]
        public ActionResult Products(string searchValue = "")
        {
            var products = _historyOrderProductRepo.GetProducts(searchValue);

            return Json(products, JsonRequestBehavior.AllowGet);
        }

        // POST: HistoryOrderProduct/Edit
        [HttpPost]
        [ActionName(ActionEdit)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA3147:Mark Verb Handlers With Validate Antiforgery Token", Justification = "<Pending>")]
        public ActionResult Edit(long productId = 0, long orderId = 0)
        {
            if (productId == 0 || orderId == 0)
            {
                return Json(ConstantsReturnMessengerOrder.ErrorNull, JsonRequestBehavior.AllowGet);
            }

            var currentEmployee = ConvertUtils<Employee>.Deserialize(User.Identity.Name);
            var employeeID = currentEmployee.EmployeeID;
            var order = _orderRepo.GetOrder(orderId);

            if (order == null)
            {
                return Json(ConstantsReturnMessengerOrder.OrderIsNotFound, JsonRequestBehavior.AllowGet);
            }

            if (order.Status != OrderStatus.PENDING.Code)
            {
                return Json(ConstantsReturnMessengerOrder.NotHandle, JsonRequestBehavior.AllowGet);
            }

            // check current balance bigger than product price
            var productInfo = _productRepo.GetProduct(productId);
            var prevBalance = order.TotalMoney + currentEmployee.Coin;

            if (prevBalance < productInfo.Price)
            {
                return Json(ConstantsReturnMessengerOrder.NotEnoughMoney, JsonRequestBehavior.AllowGet);
            }

            var newBalance = prevBalance - productInfo.Price;
            _historyOrderProductRepo.UpdateCoin(employeeID, newBalance);

            bool editOrder = _historyOrderProductRepo.EditOrder(employeeID, productId, orderId);
            if (!editOrder)
            {
                return Json(ConstantsReturnMessengerOrder.ErrorNull, JsonRequestBehavior.AllowGet);
            }

            var employee = _accountRepo.GetEmployee(currentEmployee.UserName);
            var newCurrentEmployee = ConvertUtils<Employee>.Serialize(employee);
            FormsAuthentication.SignOut();
            FormsAuthentication.SetAuthCookie(newCurrentEmployee, false);

            return Json(ConstantsReturnMessengerOrder.SuccessOrder, JsonRequestBehavior.AllowGet);
        }

        // POST: HistoryOrderProduct/Canceled
        [HttpPost]
        [ActionName(ActionCanceled)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA3147:Mark Verb Handlers With Validate Antiforgery Token", Justification = "<Pending>")]
        public ActionResult Canceled(long orderId = 0)
        {
            if (orderId == 0)
            {
                return Json(ConstantsReturnMessengerOrder.ErrorNull, JsonRequestBehavior.AllowGet);
            }

            var order = _orderRepo.GetOrder(orderId);

            if (order.Status == OrderStatus.PENDING.Code)
            {
                var cancel = _historyOrderProductRepo.Cancel(order.OrderID);
                if (!cancel)
                {
                    return Json(ConstantsReturnMessengerOrder.FaildCancelOrder, JsonRequestBehavior.AllowGet);
                }

                if (order.Type == PayTypeConstant.WALLET)
                {
                    var newBalance = order.TotalMoney + order.Employee.Coin;
                    _historyOrderProductRepo.UpdateCoin(order.Employee.EmployeeID, newBalance);
                    var employee = _accountRepo.GetEmployee(order.Employee.UserName);
                    var newCurrentEmployee = ConvertUtils<Employee>.Serialize(employee);
                    FormsAuthentication.SignOut();
                    FormsAuthentication.SetAuthCookie(newCurrentEmployee, false);
                }
            }

            return Json(ConstantsReturnMessengerOrder.CancelOrder, JsonRequestBehavior.AllowGet);
        }

        #endregion
    }
}
