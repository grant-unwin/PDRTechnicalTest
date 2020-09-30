using Microsoft.EntityFrameworkCore.Internal;
using PDR.PatientBooking.Data;
using PDR.PatientBooking.Service.BookingServices.Requests;
using PDR.PatientBooking.Service.Helpers;
using PDR.PatientBooking.Service.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PDR.PatientBooking.Service.BookingServices.Validation
{
    public class AddBookingRequestValidator : IAddBookingRequestValidator
    {
        private readonly PatientBookingContext _context;

        public AddBookingRequestValidator(PatientBookingContext context)
        {
            _context = context;
        }

        public PdrValidationResult ValidateRequest(AddBookingRequest request)
        {
            var result = new PdrValidationResult(true);

            if (BookingInThePast(request, ref result))
                return result;

            if (DoctorAlreadyBooked(request, ref result))
                return result;

            return result;
        }

        private bool DoctorAlreadyBooked(AddBookingRequest request, ref PdrValidationResult result)
        {
            if (_context.Order.Any(x =>
                x.Cancelled != true &&
                x.DoctorId == request.DoctorId &&
                x.StartTime < request.EndTime &&
                request.StartTime < x.EndTime)
            )
            {
                result.PassedValidation = false;
                result.Errors.Add("A booking with this doctor already exists during this time");
                return true;
            }

            return false;
        }

        private bool BookingInThePast(AddBookingRequest request, ref PdrValidationResult result)
        {
            if (request.StartTime < DateHelpers.CurrentDate())
            {
                result.PassedValidation = false;
                result.Errors.Add("Booking is in the past");
                return true;
            }
            return false;

        }



    }
}
