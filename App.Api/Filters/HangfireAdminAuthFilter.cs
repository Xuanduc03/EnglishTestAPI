using Hangfire.Dashboard;

namespace App.Api.Filters
{
    /// <summary>
    /// Bộ lọc phân quyền (Authorization Filter) dành riêng cho Hangfire Dashboard.
    /// Đảm bảo chỉ những người dùng đã đăng nhập và có quyền Quản trị viên (Admin) mới được phép truy cập.
    /// </summary>
    public class HangfireAdminAuthFilter : IDashboardAuthorizationFilter
    {
        /// <summary>
        /// Kiểm tra quyền truy cập của người dùng hiện tại đối với Hangfire Dashboard.
        /// </summary>
        /// <param name="context">Ngữ cảnh của Dashboard, chứa các thông tin về HTTP request hiện tại.</param>
        /// <returns>
        /// Trả về <c>true</c> nếu người dùng đã xác thực (IsAuthenticated) VÀ có role là "Admin".
        /// Trả về <c>false</c> nếu ngược lại (truy cập sẽ bị từ chối, thường trả về lỗi 401 hoặc 403).
        /// </returns>
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            return httpContext.User.Identity?.IsAuthenticated == true
                && httpContext.User.IsInRole("Admin");
        }
    }
}
