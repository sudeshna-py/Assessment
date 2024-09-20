using Assessment.Models;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Collections.Generic;

namespace Assessment.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private static Dictionary<string, string> _orderSessionMap = new Dictionary<string, string>();

        public PaymentController(IConfiguration configuration)
        {
            _configuration = configuration;
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];


        }

        // Endpoint to generate a payment link
        [HttpPost("getpaymentlink")]
        public IActionResult GetPaymentLink([FromBody] PaymentRequest request) 
        {
            if (request.Pgname != "stripe") 
            {
                return BadRequest(new { message = "Unsupported paymnet gateway" });
            }

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string>
                {
                    "card",
                },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(request.Amount * 100), 
                            Currency = request.Currency,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                //display order ID as product name
                                Name = request.OrderID,
                            },
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment",
                SuccessUrl = "https://localhost:5001/success",
                CancelUrl = "https://localhost:5001/cancel",
            };

            var service = new SessionService();
            Session session = service.Create(options);

            //Map orderID to sessionId
            _orderSessionMap[request.OrderID] = session.Id;

            return Ok(new { payment_link = session.Url });

        }

        // endpoint to check payment status
        [HttpPost("checkpaymentstatus")]
        public IActionResult CheckPaymentStatus([FromBody] PaymentStatusRequest request) 
        { 
            if (!_orderSessionMap.ContainsKey(request.OrderID))
            {
                return NotFound(new { message = "order not found" });
            }

            var sessionId = _orderSessionMap[request.OrderID];
            var service = new SessionService();
            var session = service.Get(sessionId);

            //determine payment status based on session's payment status
            var paymentStatus = session.PaymentStatus == "paid" ? "Complete" : "Pending";

            return Ok(new
            {
                orderID = request.OrderID,
                status = paymentStatus,
                paymentAmount = session.AmountTotal / 100.0 

            });


        }
    }
}
