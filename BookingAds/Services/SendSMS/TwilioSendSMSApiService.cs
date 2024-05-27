using System;
using BookingAds.Services.Abstractions;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace BookingAds.Services
{
    public class TwilioSendSMSApiService : ISendSMSService
    {
        private const string FromPhone = "+15075775721";
        private const string AccountSid = "AC786527484deacb841026d287213fb156";
        private const string AuthToken = "d0cf06bf176f1ea00b840390d021b072";

        public string SendSMS(string phone, string msg)
        {
            try
            {
                // init twilio client
                TwilioClient.Init(AccountSid, AuthToken);

                // set message options
                var fromPhone = new PhoneNumber(FromPhone);
                var toPhone = new PhoneNumber(phone);
                var messageOptions = new CreateMessageOptions(toPhone)
                {
                    From = fromPhone,
                    Body = msg,
                };

                // create message
                var message = MessageResource.Create(messageOptions);

                // test
                Console.WriteLine($"From phone {fromPhone} send sms to phone {toPhone} with content: {message.Body}");

                return message.Body;
            }
            catch
            {
                return null;
            }
        }
    }
}