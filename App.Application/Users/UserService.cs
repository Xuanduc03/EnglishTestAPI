using App.Application.Interfaces;
using App.Domain.Entities;
using App.Domain.Identity;
using AutoMapper;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace App.Application.Users
{

    public class UserService : IUserService
    {
        private readonly IRepository<User> _repository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

       public UserService(IRepository<User> repository, IMapper mapper, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            if(id == Guid.Empty)
                throw new ArgumentNullException(nameof(id));
            var user = await _repository.SelectById(id);
            if (user == null)
                throw new ArgumentException("Không tìm thấy Người dùng hợp lệ");
            return _mapper.Map<UserDto>(user);
        }

        public async Task<UserDto?> GetByEmailAsync(string email, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentException("Email không hợp lệ");

            var user = await _repository.SelectOne(x => x.Email == email, ct);
            if (user == null) throw new Exception("Không tìm thấy Email hợp lệ");
            return _mapper.Map<UserDto>(user);
        }


        public async Task<IList<UserDto>> GetByIdsAsync(Guid[] ids, CancellationToken ct = default)
        {
            if (!ids.Any())
                throw new ArgumentException("Id Không hợp lệ");
            var users = await _repository.Select(x => ids.Contains(x.Id));
            if (users == null)
                throw new Exception("Không tồn tại user nào cả");
            return _mapper.Map<List<UserDto>>(users);
        }

        public async Task<(IList<UserDto> Items, int Total)> GetAllAsync( GetUsersFilter filter, CancellationToken ct = default)
        {
            Expression<Func<User, bool>> where = x =>
                (string.IsNullOrEmpty(filter.Email) || x.Email.Contains(filter.Email)) &&
                (string.IsNullOrEmpty(filter.Fullname) || x.FullName.Contains(filter.Fullname)) &&
                (!filter.IsActive.HasValue || x.IsActive == filter.IsActive.Value);

            var result = await _repository.SelectPaged(
                where,
                filter.Page,
                filter.PageSize,
                ct);

            var items = _mapper.Map<List<UserDto>>(result.Items);

            return (items, result.Total);
        }
        // Ghi
        public async Task<UserDto> CreateAsync(CreateUserDto dto, CancellationToken ct = default)
        {
            ValidateUserInput(dto.Email, dto.Fullname, dto.Phone);
            if (string.IsNullOrWhiteSpace(dto.Password))
                throw new ArgumentException("Mật khẩu không được để trống");

            var emailExists = await _repository.SelectOne(x => x.Email.ToLower() == dto.Email.ToLower(), ct) != null;
            if (emailExists) throw new Exception("Email đã tồn tại, vui lòng thử lại");
            var newUser = new User
            {
                Id = Guid.NewGuid(),
                Email = dto.Email.Trim().ToLower(),
                FullName = dto.Fullname?.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Phone = dto.Phone?.Trim(),
                IsActive = true,
                FailedLoginAttempts = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            await _repository.Insert(newUser, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return _mapper.Map<UserDto>(newUser);

        }
        public async Task UpdateAsync(Guid id, UpdateUserDto dto, CancellationToken ct = default)
        {
            var user = await _repository.SelectOne(x => x.Id == id, ct);
            if (user == null) throw new Exception("Không tồn tại Người dùng");

            ValidateUserInput(dto.Email, dto.Fullname, dto.Phone);

            _mapper.Map(dto, user);

            await _repository.Update(user, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }
        public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Id không hợp lệ", nameof(id));

            var user = await _repository.SelectOne(x => x.Id == id);
            if (user == null) throw new KeyNotFoundException("không tồn tại người dùng");
            await _repository.DeleteWhere(x => x.Id == id, ct); 
        }
        public async Task RestoreAsync(Guid id, CancellationToken ct = default)
        {
            var user = await _repository.SelectOne(x => x.Id == id, ct);

            if (user == null)
                throw new KeyNotFoundException("Không tồn tại Người dùng");

            user.IsActive = true;

            await _repository.Update(user, ct);
        }
        public async Task ChangeRoleAsync(Guid id, UserRole newRole, CancellationToken ct = default)
        {
            var user = await _repository.SelectById(id, ct);

            if (user == null)
                throw new KeyNotFoundException("Không tồn tại Người dùng");

            user.Role = newRole;

            await _repository.Update(user, ct);
        }
        public async Task ToggleActiveAsync(Guid id, bool isActive, CancellationToken ct = default)
        {
            var user = await _repository.SelectById(id, ct);

            if (user == null)
                throw new KeyNotFoundException("Không tồn tại Người dùng");

            user.IsActive = isActive;

            await _repository.Update(user, ct);
        }

        private void ValidateUserInput(string email, string? fullname, string? phone)
        {
            var errors = new List<string>();

            // Email validation
            if (string.IsNullOrWhiteSpace(email))
                errors.Add("Email không được để trống");
            else if (!IsValidEmail(email))
                errors.Add("Email không hợp lệ");

            // Username validation
            if (string.IsNullOrWhiteSpace(fullname))
                errors.Add("Username không được để trống");
            else if (fullname.Length > 50)
                errors.Add("Username không được vượt quá 50 ký tự");
            else if (!IsValidUsername(fullname))
                errors.Add("Username chỉ được chứa chữ cái, số và dấu gạch dưới");

            // Phone validation (optional)
            if (!string.IsNullOrWhiteSpace(phone) && !IsValidPhone(phone))
                errors.Add("Số điện thoại không hợp lệ");

            // Fullname validation (optional)
            if (!string.IsNullOrWhiteSpace(fullname) && fullname.Length > 100)
                errors.Add("Họ tên không được vượt quá 100 ký tự");

            if (errors.Any())
                throw new ArgumentException(string.Join("; ", errors));
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }


        private bool IsValidUsername(string username)
        {
            return Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$");
        }

        private bool IsStrongPassword(string password)
        {
            // Ít nhất 1 chữ hoa, 1 chữ thường, 1 số và 1 ký tự đặc biệt
            return Regex.IsMatch(password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]");
        }

        private bool IsValidPhone(string phone)
        {
            // Vietnamese phone number: 10-11 digits, start with 0
            return Regex.IsMatch(phone, @"^0\d{9,10}$");
        }

    }
}
