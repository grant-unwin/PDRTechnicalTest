using System;
using System.Linq;
using AutoFixture;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using PDR.PatientBooking.Data;
using PDR.PatientBooking.Data.Models;
using PDR.PatientBooking.Service.BookingServices;
using PDR.PatientBooking.Service.BookingServices.Requests;
using PDR.PatientBooking.Service.BookingServices.Validation;
using PDR.PatientBooking.Service.Validation;

namespace PDR.PatientBooking.Service.Tests.BookingServices
{
    [TestFixture]
    public class BookingServiceTests
    {
        private MockRepository _mockRepository;
        private IFixture _fixture;

        private PatientBookingContext _context;
        private Mock<IAddBookingRequestValidator> _validator;

        private BookingService _bookingService;

        [SetUp]
        public void SetUp()
        {
            // Boilerplate
            _mockRepository = new MockRepository(MockBehavior.Strict);
            _fixture = new Fixture();

            //Prevent fixture from generating from entity circular references
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior(1));

            // Mock setup
            _context = new PatientBookingContext(new DbContextOptionsBuilder<PatientBookingContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
            _validator = _mockRepository.Create<IAddBookingRequestValidator>();

            // Mock default
            SetupMockDefaults();

            // Sut instantiation
            _bookingService = new BookingService(
                _context,
                _validator.Object
            );
        }

        private void SetupMockDefaults()
        {
            _validator.Setup(x => x.ValidateRequest(It.IsAny<AddBookingRequest>()))
                .Returns(new PdrValidationResult(true));
        }

        [Test]
        public void AddBooking_ValidatesRequest()
        {
            //arrange
            //arrange
            var clinic = _fixture.Create<Clinic>();
            var doctor = _fixture.Create<Doctor>();
            var patient = _fixture.Create<Patient>();

            patient.Clinic = clinic;

            _context.Clinic.Add(clinic);
            _context.Patient.Add(patient);
            _context.Doctor.Add(doctor);
            _context.SaveChanges();

            var request = _fixture.Create<AddBookingRequest>();
            request.DoctorId = doctor.Id;
            request.PatientId = patient.Id;


            //act
            _bookingService.AddBooking(request);

            //assert
            _validator.Verify(x => x.ValidateRequest(request), Times.Once);
        }

        [Test]
        public void AddBooking_ValidatorFails_ThrowsArgumentException()
        {
            //arrange
            var failedValidationResult = new PdrValidationResult(false, _fixture.Create<string>());

            _validator.Setup(x => x.ValidateRequest(It.IsAny<AddBookingRequest>())).Returns(failedValidationResult);

            //act
            var exception = Assert.Throws<ArgumentException>(() => _bookingService.AddBooking(_fixture.Create<AddBookingRequest>()));

            //assert
            exception.Message.Should().Be(failedValidationResult.Errors.First());
        }

        [Test]
        public void AddBooking_AddsBookingToContextWithGeneratedId()
        {
            _context.Clinic.RemoveRange(_context.Clinic.ToList());
            _context.Doctor.RemoveRange(_context.Doctor.ToList());

            //arrange
            var clinic = _fixture.Create<Clinic>();
            var doctor = _fixture.Create<Doctor>();
            var patient = _fixture.Create<Patient>();

            patient.Clinic = clinic;

            _context.Clinic.Add(clinic);
            _context.Patient.Add(patient);
            _context.Doctor.Add(doctor);
            _context.SaveChanges();
            
            var request = _fixture.Create<AddBookingRequest>();
            request.PatientId = patient.Id;
            request.DoctorId = doctor.Id;

            var expected = new Order
            {
                Id = request.Id,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                PatientId = request.PatientId,
                DoctorId = request.DoctorId,
                Patient = null,
                Doctor = null,
                SurgeryType = (int)patient.Clinic.SurgeryType
            };

            //act
            _bookingService.AddBooking(request);

            //assert

            _context.Order.Should().ContainEquivalentOf(expected,
                options => options
                .ExcludingNestedObjects()
                .IgnoringCyclicReferences()
                .Excluding(order => order.Id)
                .Excluding(order => order.Patient)
                .Excluding(order => order.Doctor));
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
        }
    }
}
