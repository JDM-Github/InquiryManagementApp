
namespace InquiryManagementApp.Service
{
    public class EnrollmentScheduleService
    {
        private readonly ApplicationDbContext _context;

        public EnrollmentScheduleService(ApplicationDbContext context)
        {
            _context = context;
        }

        public bool IsEnrollmentOpen()
        {
            var schedule = _context.EnrollmentSchedules.FirstOrDefault();
            if (schedule == null)
                return false;

            return schedule.StartDate <= DateTime.Now && schedule.EndDate >= DateTime.Now;
        }
    }
}
