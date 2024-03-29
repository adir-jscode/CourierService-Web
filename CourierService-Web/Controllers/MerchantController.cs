﻿using Amazon.S3.Transfer;
using Amazon.S3;
using CourierService_Web.Data;
using CourierService_Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Amazon;
using System.Text;

namespace CourierService_Web.Controllers
{
    public class MerchantController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public MerchantController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;

        }

        private void UpdateLayout()
        {

            var merchantId = HttpContext.Request.Cookies["MerchantId"];
            if (string.IsNullOrEmpty(merchantId))
            {
                return;
            }
            var merchant = _context.Merchants.Find(merchantId);
            if (merchant == null)
            {
                return;
            }
            ViewBag.TotalPickupRequest = _context.Parcels.Count(x => x.MerchantId == merchantId && x.Status == "Pickup Request");
            ViewBag.TotalDispatched = _context.Parcels.Count(x => x.MerchantId == merchantId && x.Status == "Dispatched");
            ViewBag.TotalTransit = _context.Parcels.Count(x => x.MerchantId == merchantId && x.Status == "In Transit");
            ViewBag.TotalDelivered = _context.Parcels.Count(x => x.MerchantId == merchantId && x.DeliveryId !=null);
            ViewBag.TotalExchanged = _context.Parcels.Count(x => x.MerchantId == merchantId && x.ExchangeId != null);
            ViewBag.TotalCancelled = _context.Parcels.Count(x => x.MerchantId == merchantId && x.Status == "Cancelled");
            ViewBag.TotalReturned = _context.Parcels.Count(x => x.MerchantId == merchantId && x.Status == "Returned");
            ViewBag.TotalParcel = _context.Parcels.Count(x => x.MerchantId == merchantId);

            //parcel list for the merchant
            ViewBag.ParcelList = _context.Parcels.Where(x => x.MerchantId == merchantId).ToList();

            // Today Pickup Request
            DateTime todayStart = DateTime.Today;
            DateTime tomorrowStart = todayStart.AddDays(1);
            ViewBag.TodayPickupRequest = _context.Parcels
                .Count(x => x.MerchantId == merchantId &&
                            x.PickupRequestDate >= todayStart &&
                            x.PickupRequestDate < tomorrowStart);

            // Today Dispatched Parcel
            ViewBag.TodayDispatched = _context.Parcels
                .Count(x => x.MerchantId == merchantId &&
                            x.DispatchDate >= todayStart &&
                            x.DispatchDate < tomorrowStart);

            // Today Delivered Parcel
            ViewBag.TodayDelivered = _context.Parcels
                .Count(x => x.MerchantId == merchantId &&
                            x.DeliveryParcel.DeliveryDate >= todayStart &&
                            x.DeliveryParcel.DeliveryDate < tomorrowStart);

            // Today Cancelled Parcel
            ViewBag.TodayCancelled = _context.Parcels
                .Count(x => x.MerchantId == merchantId &&
                            x.Status == "Cancelled" &&
                            x.DeliveryDate >= todayStart &&
                            x.DeliveryDate < tomorrowStart);

            // Today Returned Parcel
            ViewBag.TodayReturned = _context.Parcels
                .Count(x => x.MerchantId == merchantId &&
                            x.Status == "Returned" &&
                            x.DeliveryDate >= todayStart &&
                            x.DeliveryDate < tomorrowStart);

            //today on transit parcel
            ViewBag.TodayTransit = _context.Parcels.Count(x => x.MerchantId == merchantId && x.Status == "Transit");

            //parcel in hub
            ViewBag.ParcelInHub = _context.Parcels.Count(x => x.MerchantId == merchantId && x.Status == "Parcel In Hub");

            //parcel on the way
            ViewBag.ParcelOnTheWay = _context.Parcels.Count(x => x.MerchantId == merchantId && x.Status == "Parcel On The Way");


            //all parcel list for today
            ViewBag.TodayParcelList = _context.Parcels
                .Where(x => x.MerchantId == merchantId && x.PickupRequestDate >= todayStart && x.PickupRequestDate < tomorrowStart).Include(x => x.Rider).Include(h=>h.Hub).ToList();

            //count of all parcel for today
            ViewBag.TodayTotalParcel = _context.Parcels
                .Count(x => x.MerchantId == merchantId && x.PickupRequestDate >= todayStart && x.PickupRequestDate < tomorrowStart);

            //return parcel count
            ViewBag.ReturnParcelCount = _context.Parcels.Count(x => x.MerchantId == merchantId && x.ReturnId !=null);
            //if paymentStatus paid then merchant can see the amount only for today according to delivered parcel date
            

            var todayDeliveryCharge = _context.Parcels
    .Where(x => x.MerchantId == merchantId &&
                (x.PaymentStatus == "Paid To Merchant" || x.PaymentStatus == "Paid Delivery Charge To Merchant") &&
                (x.DeliveryParcel.DeliveryDate >= todayStart && x.DeliveryParcel.DeliveryDate < tomorrowStart) ||
                (x.ExchangeParcel.ExchangeDate >= todayStart && x.ExchangeParcel.ExchangeDate < tomorrowStart) ||
                (x.ReturnParcel.ReturnDate >= todayStart && x.ReturnParcel.ReturnDate < tomorrowStart)
                
                )
    .Sum(x => x.DeliveryCharge);

            ViewBag.TodayDeliveryCharge = todayDeliveryCharge;


            var todayExchangeReturnCharge = _context.Parcels
    .Where(x => x.MerchantId == merchantId &&
                (x.PaymentStatus == "Paid Delivery Charge To Merchant") &&
                (x.ExchangeParcel.ExchangeDate >= todayStart && x.ExchangeParcel.ExchangeDate < tomorrowStart) ||
                (x.ReturnParcel.ReturnDate >= todayStart && x.ReturnParcel.ReturnDate < tomorrowStart)

                )
    .Sum(x => x.DeliveryCharge);

           




            //ViewBag.TodayDeliveryCharge = _context.Parcels.Where(x => x.MerchantId == merchantId && x.PaymentStatus == "Paid To Merchant" || x.PaymentStatus == "Paid Delivery Charge To Merchant" && x.DeliveryParcel.DeliveryDate >= todayStart && x.DeliveryParcel.DeliveryDate < tomorrowStart).Sum(x => x.DeliveryCharge);


            //calculate total product price for today if payment status is paid & also with quantity
            var totalProductPrice = _context.Parcels.Where(x => x.MerchantId == merchantId && x.PaymentStatus == "Paid To Merchant" && x.DeliveryParcel.DeliveryDate >= todayStart && x.DeliveryParcel.DeliveryDate < tomorrowStart).Sum(x => x.ProductPrice * x.ProductQuantity);
            ViewBag.TodayProductPrice = totalProductPrice;

            //ViewBag.TodayProductPrice = _context.Parcels.Where(x => x.MerchantId == merchantId && x.PaymentStatus == "Paid" && x.DeliveryParcel.DeliveryDate >= todayStart && x.DeliveryParcel.DeliveryDate < tomorrowStart).Sum(x => x.ProductPrice);
            ViewBag.TodayPayment = totalProductPrice + todayDeliveryCharge;

            //notification
            ViewBag.Notifications = _context.NotificationsPermission.Where(x => x.ReceiverId == merchantId).OrderByDescending(x => x.Date).Include(p=>p.Parcel).Include(x=>x.Parcel.Rider).ToList();

            //amount collected by hub according to merchant id
            ViewBag.TotalAmountCollected = _context.HubPayments.Where(x => x.Hub.Merchants.Any(x => x.Id == merchantId)).Sum(x => x.TotalAmount);

            //total price sum calculation from parcel according to merchant id today
            ViewBag.TotalPriceSum = _context.Parcels.Where(x => x.MerchantId == merchantId && x.PickupRequestDate >=todayStart && x.PickupRequestDate < tomorrowStart ).Sum(x => x.TotalPrice);
            //amount paid by merchant today
            ViewBag.AmountPaid = _context.MerchantPayments.Where(x => x.MerchantId == merchantId && x.DateTime >= todayStart && x.DateTime < tomorrowStart).Sum(x => x.AmountPaid);
            //calculate due amount
            ViewBag.DueAmount = ViewBag.TotalPriceSum - ViewBag.AmountPaid;

        }
        private bool IsMerchantLoggedIn()
        {
            var merchantId = HttpContext.Request.Cookies["MerchantId"];
            if (string.IsNullOrEmpty(merchantId))
            {
                return false;
            }
            var merchant = _context.Merchants.Find(merchantId);
            if (merchant == null)
            {
                return false;
            }
            return true;
        }
        public IActionResult Index()
        {
            if (!IsMerchantLoggedIn())
            {
                return RedirectToAction("Login", "Home");
            }
            UpdateLayout();
            return View();
        }

        public IActionResult Profile()
        {
            if (!IsMerchantLoggedIn())
            {
                return RedirectToAction("Login", "Home");
            }
            var merchantId = HttpContext.Request.Cookies["MerchantId"];
            var merchant = _context.Merchants.Find(merchantId);
            return View(merchant);

        }

        //Return Parcel List
        public IActionResult ReturnParcelList(DateTime? selectedDate)
        {
            if (!IsMerchantLoggedIn())
            {
                return RedirectToAction("Login", "Home");
            }
            var merchantId = HttpContext.Request.Cookies["MerchantId"];
            IQueryable<Parcel> returnParcelsQuery = _context.Parcels
                .Where(x => x.MerchantId == merchantId && x.ReturnId != null)
                .Include(x => x.ReturnParcel)
                .Include(x => x.Rider)
                .Include(h => h.Hub);

            if(!selectedDate.HasValue)
            {
                selectedDate = DateTime.Today;
            }
            returnParcelsQuery = returnParcelsQuery.Where(x => x.ReturnParcel.ReturnDate.Date == selectedDate.Value.Date);
            var returnParcels = returnParcelsQuery.ToList();

            ViewBag.SelectedDate = selectedDate.Value.Date;

            return View(returnParcels);

            
            
        }

        public IActionResult DownloadReturnParcelsCsv(DateTime? selectedDate)
        {
            if (!IsMerchantLoggedIn())
            {
                return RedirectToAction("Login", "Home");
            }

            var merchantId = HttpContext.Request.Cookies["MerchantId"];
            IQueryable<Parcel> returnParcelsQuery = _context.Parcels
                .Where(x => x.MerchantId == merchantId && x.ReturnId != null)
                .Include(x => x.ReturnParcel)
                .Include(x => x.Rider)
                .Include(h => h.Hub);

            if (!selectedDate.HasValue)
            {
                selectedDate = DateTime.Today;
            }

            returnParcelsQuery = returnParcelsQuery.Where(x => x.ReturnParcel.ReturnDate.Date == selectedDate.Value.Date);
            var returnParcels = returnParcelsQuery.ToList();

            // Generate CSV content
            StringBuilder csvContent = new StringBuilder();
            csvContent.AppendLine("ID,Hub,Rider,Pickup Location,Pickup Request Date,Return Date,Receiver Name,Receiver Address,Receiver Contact,Product Name,Product Weight,Product Price,Product Quantity,Delivery Type,Delivery Charge,Total Price,Status,Payment Status");
            foreach (var parcel in returnParcels)
            {
                csvContent.AppendLine($"{parcel.ReturnId}, {parcel.Hub?.Name ?? "Not Assigned"}, {parcel.Rider?.Name ?? "Not Assigned"}, {parcel.PickupLocation}, {parcel.PickupRequestDate?.ToString("M/d/yyyy, h:mm tt")}, {parcel.ReturnParcel.ReturnDate.ToString("M/d/yyyy, h:mm tt")}, {parcel.ReceiverName}, {parcel.ReceiverAddress}, {parcel.ReceiverContactNumber}, {parcel.ProductName}, {parcel.ProductWeight}, {parcel.ProductPrice}, {parcel.ProductQuantity}, {parcel.DeliveryType}, {parcel.DeliveryCharge}, {parcel.TotalPrice}, {parcel.Status}, {parcel.PaymentStatus}");
            }

            // Return CSV file
            return File(Encoding.UTF8.GetBytes(csvContent.ToString()), "text/csv", $"Return_Parcels_{selectedDate.Value.ToString("yyyy-MM-dd")}.csv");
        }

        //Exchange Parcel List
        public IActionResult ExchangeParcelList(DateTime? selectedDate)
        {
            if (!IsMerchantLoggedIn())
            {
                return RedirectToAction("Login", "Home");
            }
            var merchantId = HttpContext.Request.Cookies["MerchantId"];
            IQueryable<Parcel> exchangeParcelsQuery = _context.Parcels
                .Where(x => x.MerchantId == merchantId && x.ExchangeId != null)
                .Include(x => x.ExchangeParcel)
                .Include(x => x.Rider)
                .Include(h => h.Hub);
            if(!selectedDate.HasValue)
            {
                selectedDate = DateTime.Today;
            }
            exchangeParcelsQuery = exchangeParcelsQuery.Where(x => x.ExchangeParcel.ExchangeDate.Date == selectedDate.Value.Date);
            var exchangeParcels = exchangeParcelsQuery.ToList();
            ViewBag.SelectedDate = selectedDate.Value.Date;
            return View(exchangeParcels);

         
        }

        public IActionResult DownloadExchangeCsv(DateTime? selectedDate)
        {
            if (!IsMerchantLoggedIn())
            {
                return RedirectToAction("Login", "Home");
            }

            
            var merchantId = HttpContext.Request.Cookies["MerchantId"];
            IQueryable<Parcel> exchangeParcelsQuery = _context.Parcels
                .Where(x => x.MerchantId == merchantId && x.ExchangeId != null)
                .Include(x => x.ExchangeParcel)
                .Include(x => x.Rider)
                .Include(h => h.Hub);

            if (!selectedDate.HasValue)
            {
                selectedDate = DateTime.Today;
            }

            exchangeParcelsQuery = exchangeParcelsQuery.Where(x => x.ExchangeParcel.ExchangeDate.Date == selectedDate.Value.Date);

            var exchangeParcels = exchangeParcelsQuery.ToList();

            
            StringBuilder csvContent = new StringBuilder();

            
            csvContent.Append("\uFEFF");

            
            csvContent.AppendLine("ID, Hub, Rider, Pickup Location, Pickup Request Date, Exchange Date, Receiver Name, Receiver Address, Receiver Contact, Product Name, Product Weight, Product Price, Product Quantity, Delivery Type, Delivery Charge, Total Price, Status, Payment Status");

            
            foreach (var parcel in exchangeParcels)
            {
                csvContent.AppendLine($"{parcel.Id}, {parcel.Hub?.Name ?? "Not Assigned"}, {parcel.Rider?.Name ?? "Not Assigned"}, {parcel.PickupLocation}, {parcel.PickupRequestDate?.ToString("M/d/yyyy, h:mm tt")}, {parcel.ExchangeParcel.ExchangeDate.ToString("M/d/yyyy, h:mm tt")}, {parcel.ReceiverName}, {parcel.ReceiverAddress}, {parcel.ReceiverContactNumber}, {parcel.ProductName}, {parcel.ProductWeight}, {parcel.ProductPrice}, {parcel.ProductQuantity}, {parcel.DeliveryType}, {parcel.DeliveryCharge}, {parcel.TotalPrice}, {parcel.Status}, {parcel.PaymentStatus}");
            }

            // Return CSV file
            return File(Encoding.UTF8.GetBytes(csvContent.ToString()), "text/csv", $"ExchangeParcels_{selectedDate.Value.ToString("yyyy-MM-dd")}.csv");
        }


        //delivered parcel list
        public IActionResult DeliveredParcelList(DateTime? selectedDate)
        {
            if (!IsMerchantLoggedIn())
            {
                return RedirectToAction("Login", "Home");
            }

            var merchantId = HttpContext.Request.Cookies["MerchantId"];
            IQueryable<Parcel> deliveredParcelsQuery = _context.Parcels
                .Where(x => x.MerchantId == merchantId && x.DeliveryId != null)
                .Include(x => x.DeliveryParcel)
                .Include(x => x.Rider)
                .Include(h => h.Hub);

            if (!selectedDate.HasValue)
            {
                selectedDate = DateTime.Today;
            }
            
                deliveredParcelsQuery = deliveredParcelsQuery.Where(x => x.DeliveryParcel.DeliveryDate.Date == selectedDate.Value.Date);
                var deliveredParcels = deliveredParcelsQuery.ToList();

                ViewBag.SelectedDate = selectedDate.Value.Date;
                return View(deliveredParcels);

           

            
        }

        public IActionResult DownloadDeliveredParcelsCsv(DateTime? selectedDate)
        {
            if (!IsMerchantLoggedIn())
            {
                return RedirectToAction("Login", "Home");
            }

            var merchantId = HttpContext.Request.Cookies["MerchantId"];
            IQueryable<Parcel> deliveredParcelsQuery = _context.Parcels
                .Where(x => x.MerchantId == merchantId && x.DeliveryId != null)
                .Include(x => x.DeliveryParcel)
                .Include(x => x.Rider)
                .Include(h => h.Hub);

            if (!selectedDate.HasValue)
            {
                selectedDate = DateTime.Today;
            }

            deliveredParcelsQuery = deliveredParcelsQuery.Where(x => x.DeliveryParcel.DeliveryDate.Date == selectedDate.Value.Date);
            var deliveredParcels = deliveredParcelsQuery.ToList();

            // Generate CSV content
            StringBuilder csvContent = new StringBuilder();
            csvContent.AppendLine("ID,Hub,Rider,Pickup Location,Pickup Request Date,Delivery Date,Receiver Name,Receiver Address,Receiver Contact,Product Name,Product Weight,Product Price,Product Quantity,Delivery Type,Delivery Charge,Total Price,Status");
            foreach (var parcel in deliveredParcels)
            {
                // Ensure proper formatting of text fields by enclosing them in double quotes
                csvContent.AppendLine($"\"{parcel.Id}\",\"{parcel.Hub?.Name ?? "Not Assigned"}\",\"{parcel.Rider?.Name ?? "Not Assigned"}\",\"{parcel.PickupLocation}\",\"{parcel.PickupRequestDate?.ToString("M/d/yyyy, h:mm tt")}\",\"{parcel.DeliveryParcel.DeliveryDate.ToString("M/d/yyyy, h:mm tt")}\",\"{parcel.ReceiverName}\",\"{parcel.ReceiverAddress}\",\"{parcel.ReceiverContactNumber}\",\"{parcel.ProductName}\",{parcel.ProductWeight},{parcel.ProductPrice},{parcel.ProductQuantity},\"{parcel.DeliveryType}\",{parcel.DeliveryCharge},{parcel.TotalPrice},\"{parcel.Status}\"");
            }

            // Return CSV file
            return File(Encoding.UTF8.GetBytes(csvContent.ToString()), "text/csv", $"Delivered_Parcels_{selectedDate.Value.ToString("yyyy-MM-dd")}.csv");
        }





        //update profile
        [HttpPost]
        public IActionResult UpdateProfile(Merchant merchant, IFormFile? file)
        {
            if (!IsMerchantLoggedIn())
            {
                return RedirectToAction("Login", "Home");
            }

            var merchantId = HttpContext.Request.Cookies["MerchantId"];
            if (string.IsNullOrEmpty(merchantId))
            {

                return RedirectToAction("Login", "Home");
            }

            var merchantToUpdate = _context.Merchants.Find(merchantId);
            if (merchantToUpdate == null)
            {

                return RedirectToAction("Login", "Home");
            }

            if (ModelState.IsValid)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                if (file != null && file.Length > 0)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string riderPath = Path.Combine(wwwRootPath, "Images", "Merchant");

                    // Delete old image if it exists
                    if (!string.IsNullOrEmpty(merchantToUpdate.ImageUrl))
                    {
                        string oldImagePath = Path.Combine(wwwRootPath, merchantToUpdate.ImageUrl.TrimStart('~', '/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }

                        //delete from aws s3
                        string[] split = merchantToUpdate.ImageUrl.Split("/");
                        string key = "Merchant/" + split[split.Length - 1];
                        var s3Client = new AmazonS3Client("AKIAU6GDYMTHTIZML6UG", "9Mjr5N26gAtUX6aOyGBNy688zMgP9Dt46ndJOIh/", RegionEndpoint.USEast1);
                        var fileTransferUtility = new TransferUtility(s3Client);
                        fileTransferUtility.S3Client.DeleteObjectAsync("courierbuckets3", key);
                    }

                    // Save new image
                    using (var fileStream = new FileStream(Path.Combine(riderPath, fileName), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }

                    //upload to aws s3
                    var news3Client = new AmazonS3Client("AKIAU6GDYMTHTIZML6UG", "9Mjr5N26gAtUX6aOyGBNy688zMgP9Dt46ndJOIh/", RegionEndpoint.USEast1);
                    var updatedfileTransferUtility = new TransferUtility(news3Client);
                    var uploadRequest = new TransferUtilityUploadRequest
                    {
                        FilePath = riderPath + "\\" + fileName,
                        BucketName = "courierbuckets3",
                        Key = "Merchant/" + fileName,
                        CannedACL = S3CannedACL.PublicRead
                    };
                    updatedfileTransferUtility.Upload(uploadRequest);
                    //after upload delete from local storage
                    System.IO.File.Delete(riderPath + "\\" + fileName);
                    merchantToUpdate.ImageUrl = "https://courierbuckets3.s3.amazonaws.com/Merchant/" + fileName;


                }

                // Update other rider information
                merchantToUpdate.Name = merchant.Name;
                merchantToUpdate.CompanyName = merchant.CompanyName;


                merchantToUpdate.Email = merchant.Email;
                merchantToUpdate.ContactNumber = merchant.ContactNumber;
                merchantToUpdate.FullAddress = merchant.FullAddress;

                _context.Merchants.Update(merchantToUpdate);
                _context.SaveChanges();
                return RedirectToAction("Profile");
            }

            // If ModelState is not valid, return to the profile page with validation errors
            return View("Profile", merchant);
        }



        public IActionResult AddNewParcel()
        {
            if (!IsMerchantLoggedIn())
            {
                return RedirectToAction("Login", "Home");
            }
            //parcel with hub information
            ViewBag.HubList = _context.Hubs.ToList();
            var merchantId = HttpContext.Request.Cookies["MerchantId"];
           //merchant area
           var merchant = _context.Merchants.Find(merchantId);
            ViewBag.MerchantArea = merchant.Area;
            //merhcant full address
            ViewBag.MerchantFullAddress = merchant.FullAddress;
            return View();

            
        }
        [HttpPost]
        public IActionResult AddParcel(Parcel parcel)
        {
            if (!IsMerchantLoggedIn())
            {
                return RedirectToAction("Login", "Home");
            }

            if (!ModelState.IsValid)
            {
                return View(parcel);
            }

            _context.Parcels.Add(parcel);
            _context.SaveChanges();
            TempData["success"] = "Parcel Added Successfully";
            return RedirectToAction("Index");
        }

        public IActionResult Parcels(DateTime? startDate, DateTime? endDate)
        {
            if (!IsMerchantLoggedIn())
            {
                return RedirectToAction("Login", "Home");
            }

            var merchantId = HttpContext.Request.Cookies["MerchantId"];
            IQueryable<Parcel> parcelsQuery = _context.Parcels
                .Where(x => x.MerchantId == merchantId)
                .Include(u => u.Rider)
                .Include(h => h.Hub);

            // If both start date and end date are provided, filter by date range
            if (startDate.HasValue && endDate.HasValue)
            {
                parcelsQuery = parcelsQuery.Where(x => x.PickupRequestDate.Value.Date >= startDate.Value.Date && x.PickupRequestDate.Value.Date <= endDate.Value.Date);
            }
            // If only start date is provided, filter from start date to today
            else if (startDate.HasValue)
            {
                parcelsQuery = parcelsQuery.Where(x => x.PickupRequestDate.Value.Date >= startDate.Value.Date && x.PickupRequestDate.Value.Date <= DateTime.Today);
            }
            // If only end date is provided, filter from the beginning to end date
            else if (endDate.HasValue)
            {
                parcelsQuery = parcelsQuery.Where(x => x.PickupRequestDate.Value.Date <= endDate.Value.Date);
            }
            // If no date range is provided, default to today
            else
            {
                parcelsQuery = parcelsQuery.Where(x => x.PickupRequestDate.Value.Date == DateTime.Today);
            }

            var parcels = parcelsQuery.ToList();

            // Pass selected date range to the ViewBag
            ViewBag.StartDate = startDate ?? DateTime.Today;
            ViewBag.EndDate = endDate ?? DateTime.Today;

            return View(parcels);
        }


        public IActionResult DownloadCsv(DateTime? startDate, DateTime? endDate)
        {
            if (!IsMerchantLoggedIn())
            {
                return RedirectToAction("Login", "Home");
            }

            // Get the list of parcels based on the selected date range
            var merchantId = HttpContext.Request.Cookies["MerchantId"];
            IQueryable<Parcel> parcelsQuery = _context.Parcels
                .Where(x => x.MerchantId == merchantId)
                .Include(u => u.Rider)
                .Include(h => h.Hub);

            // Filter parcels by date range
            if (startDate.HasValue && endDate.HasValue)
            {
                parcelsQuery = parcelsQuery.Where(x => x.PickupRequestDate.Value.Date >= startDate.Value.Date && x.PickupRequestDate.Value.Date <= endDate.Value.Date);
            }
            else if (startDate.HasValue)
            {
                parcelsQuery = parcelsQuery.Where(x => x.PickupRequestDate.Value.Date >= startDate.Value.Date);
            }
            else if (endDate.HasValue)
            {
                parcelsQuery = parcelsQuery.Where(x => x.PickupRequestDate.Value.Date <= endDate.Value.Date);
            }
            else
            {
                // Default to today if no date range is provided
                parcelsQuery = parcelsQuery.Where(x => x.PickupRequestDate.Value.Date == DateTime.Today);
            }

            var parcels = parcelsQuery.ToList();

            // Generate CSV content
            StringBuilder csvContent = new StringBuilder();

            // Column headers
            csvContent.AppendLine("ID,Hub,Rider,Pickup Location,Pickup Request Date,Receiver Name,Receiver Address,Receiver Contact,Product Name,Product Weight,Product Price,Product Quantity,Delivery Type,Delivery Charge,Total Price,Status,Payment Status");

            // Data rows
            foreach (var parcel in parcels)
            {
                // Ensure proper formatting of text fields by enclosing them in double quotes
                csvContent.AppendLine($"\"{parcel.Id}\",\"{parcel.Hub?.Name ?? "Not Assigned"}\",\"{parcel.Rider?.Name ?? "Not Assigned"}\",\"{parcel.PickupLocation}\",\"{parcel.PickupRequestDate?.ToString("M/d/yyyy, h:mm tt")}\",\"{parcel.ReceiverName}\",\"{parcel.ReceiverAddress}\",\"{parcel.ReceiverContactNumber}\",\"{parcel.ProductName}\",{parcel.ProductWeight},{parcel.ProductPrice},{parcel.ProductQuantity},\"{parcel.DeliveryType}\",{parcel.DeliveryCharge},{parcel.TotalPrice},\"{parcel.Status}\",\"{parcel.PaymentStatus}\"");
            }

            // Return CSV file
            return File(Encoding.UTF8.GetBytes(csvContent.ToString()), "text/csv", $"Parcels_{startDate?.ToString("yyyy-MM-dd")}_{endDate?.ToString("yyyy-MM-dd")}.csv");
        }







        //change parcel status to ReturnParcelReceived
        public IActionResult ReturnParcelReceived(string id)
        {
            if (!IsMerchantLoggedIn())
            {
                return RedirectToAction("Login", "Home");
            }
            var parcel = _context.Parcels.Find(id);
            if (parcel == null)
            {
                return NotFound();
            }
            parcel.Status = "Return Parcel Received";
            _context.SaveChanges();
            TempData["success"] = "Parcel Status Changed to Return Parcel Received";
            return RedirectToAction("Parcels");
        }
        //ExchangeParcelReceived
        public IActionResult ExchangeParcelReceived(string id)
        {
            if (!IsMerchantLoggedIn())
            {
                return RedirectToAction("Login", "Home");
            }
            var parcel = _context.Parcels.Find(id);
            if (parcel == null)
            {
                return NotFound();
            }
            parcel.Status = "Exchange Parcel Received";
            _context.SaveChanges();
            TempData["success"] = "Exchange Parcel Received";
            return RedirectToAction("Parcels");
        }

        public IActionResult ChangePassword()
        {
            if (!IsMerchantLoggedIn())
            {
                return RedirectToAction("Login", "Home");
            }
            return View();
        }
        [HttpPost]
        public IActionResult ChangePassword(ResetPassword resetPassword)
        {
            if (resetPassword == null)
            {
                return NotFound();
            }
            //adminid from cookie

            if (ModelState.IsValid)
            {
                var merchant = _context.Merchants.Find(resetPassword.Id);
                if (merchant == null)
                {
                    return NotFound();
                }
                if (merchant.Password == resetPassword.OldPassword && resetPassword.NewPassword == resetPassword.ConfirmPassword)
                {
                    merchant.Password = resetPassword.NewPassword;
                    _context.SaveChanges();
                    TempData["success"] = "Password Changed Successfully";
                    return RedirectToAction("Index");
                }

                else if (merchant.Password != resetPassword.OldPassword)
                {
                    TempData["error"] = "Old Password is Incorrect";
                    return View(resetPassword);
                }
                else if (resetPassword.NewPassword != resetPassword.ConfirmPassword)
                {
                    TempData["error"] = "New Password and Confirm Password does not match";
                    return View(resetPassword);
                }
                else
                {
                    return View(resetPassword);
                }
            }
            else
            {
                return View(resetPassword);
            }

        }

        //complain
        public IActionResult Complain()
        {
            if (!IsMerchantLoggedIn())
            {
                return RedirectToAction("Login", "Home");
            }
            var merchantId = HttpContext.Request.Cookies["MerchantId"];
            var complains = _context.Complain.Where(x => x.MerchantId == merchantId).ToList();
            return View(complains);
        }

        public IActionResult AddComplain()
        {
            if (!IsMerchantLoggedIn())
            {
                return RedirectToAction("Login", "Home");
            }

            return View();
        }
        [HttpPost]
        public IActionResult AddComplain(Complain complain)
        {
            if (!IsMerchantLoggedIn())
            {
                return RedirectToAction("Login", "Home");
            }
            if (!ModelState.IsValid)
            {
                return View(complain);
            }
            var merchantId = HttpContext.Request.Cookies["MerchantId"];
            complain.MerchantId = merchantId;
            complain.Date = DateTime.Now;
            _context.Complain.Add(complain);
            _context.SaveChanges();
            TempData["success"] = "Complain Added Successfully";
            return RedirectToAction("Complain");
        }


        [HttpPost]
        public IActionResult ApprovePermission(string parcelId,int newPrice)
        {
            
            var parcel = _context.Parcels.Find(parcelId);
            var merchantId = HttpContext.Request.Cookies["MerchantId"];

            
            if (parcel.MerchantId != merchantId)
            {
                return BadRequest("Unauthorized");
            }

            
            parcel.ProductPrice = newPrice;
            

            
            parcel.TotalPrice = (int)(parcel.ProductPrice * parcel.ProductQuantity + parcel.DeliveryCharge);
           
            var notification = _context.NotificationsPermission.Where(p=>p.ParcelId == parcelId).FirstOrDefault();
            notification.Status = 1;

            _context.SaveChanges();

            TempData["success"] = "Permission Approved Successfully";

            
            return RedirectToAction("Index");
        }

    }
}
