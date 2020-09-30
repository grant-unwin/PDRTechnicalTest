using AutoFixture;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using PDR.PatientBooking.Data;
using PDR.PatientBooking.Data.Models;
using PDR.PatientBooking.Service.BookingServices.Requests;
using PDR.PatientBooking.Service.BookingServices.Validation;
using PDR.PatientBooking.Service.ClinicServices.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace PDR.PatientBooking.Service.Tests.BookingServices.Validation
{
    public class AddBookingRequestValidatorTests
    {
        private IFixture _fixture;

        private PatientBookingContext _context;

        private AddBookingRequestValidator _addBookingRequestValidator;

        [SetUp]
        public void SetUp()
        {
            // Boilerplate
            _fixture = new Fixture();

            //Prevent fixture from generating from entity circular references 
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior(1));

            // Mock setup
            _context = new PatientBookingContext(new DbContextOptionsBuilder<PatientBookingContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

            // Sut instantiation
            _addBookingRequestValidator = new AddBookingRequestValidator(
                _context
            );
        }

        [Test]
        public void ValidateRequest_AllChecksPass_ReturnsPassedValidationResult()
        {
            //arrange
            var request = GetValidRequest();

            //act
            var res = _addBookingRequestValidator.ValidateRequest(request);

            //assert
            res.PassedValidation.Should().BeTrue();
        }

        [TestCase(0, 0, 0, 0, 60, 60, false)] // same time
        [TestCase(0, 0, 1, 0, 60, 60, true)] // after but touching
        [TestCase(1, 0, 0, 0, 60, 60, true)] // before but touching
        [TestCase(0, 0, 0, 15, 60, 15, false)] // inside
        [TestCase(0, 15, 0, 0, 15, 60, false)] // outside
        [TestCase(0, 0, 0, 15, 15, 10, true)] // before
        [TestCase(0, 15, 0, 0, 15, 10, true)] // after 
        public void ValidateRequest_BookingAlreadyTaken_ReturnsFailedValidationResult(int existingHour, int existingMin, int newHour, int newMin, int orderDuration, int newOrderDuration, bool expectedResult)
        {
            var nextYear = DateTime.Now.Year + 1;

            //arrange
            var clinic = _fixture.Create<Clinic>();
            var doctor = _fixture.Create<Doctor>();
            var patient = _fixture.Create<Patient>();
            var order = _fixture.Create<Order>();

            patient.Clinic = clinic;

            order.StartTime = new DateTime(nextYear, 1, 1, existingHour, existingMin, 0);
            order.EndTime = order.StartTime.AddMinutes(orderDuration);
            order.DoctorId = doctor.Id;

            _context.Doctor.Add(doctor);
            _context.Clinic.Add(clinic);
            _context.Patient.Add(patient);
            _context.Order.Add(order);

            _context.SaveChanges();

            var request = GetValidRequest();
            request.StartTime = new DateTime(nextYear, 1, 1, newHour, newMin, 0);
            request.EndTime = request.StartTime.AddMinutes(newOrderDuration);
            request.DoctorId = order.DoctorId;

            //act
            var res = _addBookingRequestValidator.ValidateRequest(request);

            //assert
            res.PassedValidation.Should().Equals(expectedResult);
            if (!expectedResult)
            {
                res.Errors.Should().Contain("A booking with this doctor already exists during this time");
            }
        }

        [Test]
        public void ValidateRequest_BookingInThePast_ReturnsFailedValidationResult()
        {
            //arrange
            var request = GetValidRequest();
            request.StartTime = new DateTime(DateTime.Now.Year - 1, 1, 1, 1, 1, 0);
            request.EndTime = request.StartTime.AddMinutes(60);

            _context.SaveChanges();

            //act
            var res = _addBookingRequestValidator.ValidateRequest(request);

            //assert
            res.PassedValidation.Should().Equals(false);

            res.Errors.Should().Contain("Booking is in the past");    
        }

        private AddBookingRequest GetValidRequest()
        {
            var request = _fixture.Create<AddBookingRequest>();
            request.StartTime = new DateTime(DateTime.Now.Year + 1, 1, 1, 1, 1, 0);
            request.EndTime = request.StartTime.AddHours(1);
            return request;
        }
    }
}
