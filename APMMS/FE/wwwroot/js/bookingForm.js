// bookingForm.js - logic cho modal đặt lịch công khai / đăng nhập

(function () {
    const REQUIRED_DATA_KEY = 'original-required';

    function isTokenValid(token) {
        if (!token) return false;
        try {
            const payload = JSON.parse(atob(token.split('.')[1]));
            // Kiểm tra expiration time (exp)
            if (payload.exp) {
                const expirationTime = payload.exp * 1000; // Convert to milliseconds
                const currentTime = Date.now();
                if (currentTime >= expirationTime) {
                    console.log('⚠️ Token has expired, clearing from localStorage');
                    localStorage.removeItem('authToken');
                    localStorage.removeItem('userInfo');
                    return false;
                }
            }
            return true;
        } catch (error) {
            console.error('Error validating token:', error);
            // Nếu không decode được, coi như token không hợp lệ
            localStorage.removeItem('authToken');
            localStorage.removeItem('userInfo');
            return false;
        }
    }

    function getUserIdFromToken(token) {
        if (!token) return null;
        try {
            const payload = JSON.parse(atob(token.split('.')[1]));
            return payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ||
                   payload['UserId'] ||
                   payload['nameid'];
        } catch (error) {
            console.error('Error decoding token:', error);
            return null;
        }
    }

    function ensureLicensePlateInput() {
        let field = $('#licensePlate');
        if (!field.length) {
            field = $('<input type="text" class="form-control" id="licensePlate" placeholder="VD: 30A-12345" autocomplete="off" required pattern="^[0-9]{2}[A-Z]{1,2}-[0-9]{4,5}$" maxlength="15">');
            $('#bookingForm .modal-body .row .col-md-6').last().prepend(field);
        }
        if (field.is('select')) {
            const newInput = $('<input type="text" class="form-control" id="licensePlate" placeholder="VD: 30A-12345" autocomplete="off" required pattern="^[0-9]{2}[A-Z]{1,2}-[0-9]{4,5}$" maxlength="15">');
            field.replaceWith(newInput);
            field = newInput;
        }
        // Ensure required attribute is set
        field.prop('required', true);
        return field;
    }

    function ensureLicensePlateSelect(cars = []) {
        let field = $('#licensePlate');
        if (field.is('input')) {
            const newSelect = $('<select class="form-select" id="licensePlate" required></select>');
            field.replaceWith(newSelect);
            field = newSelect;
        }
        field.empty();
        if (cars.length > 0) {
            field.append('<option value="">-- Chọn biển số xe --</option>');
            cars.forEach(car => {
                // Handle both camelCase and PascalCase property names
                const carId = car.id || car.Id;
                const plate = car.licensePlate || car.LicensePlate || 'Chưa có biển số';
                const name = (car.carName || car.CarName) ? ` - ${car.carName || car.CarName}` : '';
                field.append(`<option value="${carId}" data-car-id="${carId}">${plate}${name}</option>`);
            });
            field.prop('disabled', false);
            console.log(`✅ Populated license plate dropdown with ${cars.length} cars`);
        } else {
            field.append('<option value="">-- Bạn chưa có xe nào --</option>');
            field.prop('disabled', true);
            console.log('⚠️ No cars to populate in license plate dropdown');
        }
        field.prop('required', true);
        return field;
    }

    function toggleGuestFields(isLoggedIn) {
        $('.guest-field').each(function () {
            const $section = $(this);
            const $inputs = $section.find('input,select,textarea');

            if (isLoggedIn) {
                $inputs.each(function () {
                    const $input = $(this);
                    if ($input.data(REQUIRED_DATA_KEY) === undefined) {
                        $input.data(REQUIRED_DATA_KEY, $input.prop('required'));
                    }
                    $input.prop('required', false);
                });
                $section.hide();
            } else {
                $inputs.each(function () {
                    const $input = $(this);
                    const originalRequired = $input.data(REQUIRED_DATA_KEY);
                    if (originalRequired !== undefined) {
                        $input.prop('required', originalRequired);
                    }
                });
                $section.show();
            }
        });
    }

    // ----------------------------------------------------------
    // API helpers
    // ----------------------------------------------------------

    window.loadBranches = async function loadBranches() {
        const token = localStorage.getItem('authToken');
        const apiBaseUrl = window.API_BASE_URL || 'https://localhost:7173/api';
        const select = $('#branch');

        if (!select.length) {
            console.warn('⚠️ Branch select element not found yet (#branch), element may not be rendered');
            // Retry sau 500ms nếu element chưa có
            setTimeout(function() {
                if ($('#branch').length) {
                    console.log('🔄 Retrying loadBranches after element found...');
                    loadBranches();
                }
            }, 500);
            return;
        }

        console.log('🔄 Loading branches... (token:', token ? 'present' : 'none', ')');
        select.prop('disabled', false);
        select.prop('required', true);
        
        // Chỉ update text nếu chưa có options (tránh xóa data đã load)
        if (select.find('option').length <= 1) {
            select.html('<option value="">-- Đang tải danh sách chi nhánh --</option>');
        }

        try {
            const headers = {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            };
            if (token) {
                headers['Authorization'] = `Bearer ${token}`;
            }

            const response = await fetch(`${apiBaseUrl}/Branch`, {
                method: 'GET',
                headers,
                mode: 'cors'
            });

            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(`HTTP error! status: ${response.status}, message: ${errorText}`);
            }

            const result = await response.json();
            const branches = result.success && Array.isArray(result.data) ? result.data : (Array.isArray(result) ? result : []);

            select.empty();
            select.append('<option value="">-- Chọn chi nhánh --</option>');

            if (!branches.length) {
                select.append('<option value="">-- Không có chi nhánh nào --</option>');
                return;
            }

            let hasDefaultBranch = false;
            const defaultBranchId = token ? getDefaultBranchId() : null;

            branches.forEach(branch => {
                const optionValue = branch.id;
                if (defaultBranchId && parseInt(defaultBranchId, 10) === parseInt(optionValue, 10)) {
                    hasDefaultBranch = true;
                }
                select.append(`<option value="${optionValue}">${branch.name || 'N/A'}</option>`);
            });

            if (hasDefaultBranch) {
                select.val(String(defaultBranchId));
            }
        } catch (error) {
            console.error('❌ Error loading branches:', error);
            select.empty();
            select.append('<option value="">-- Lỗi tải danh sách chi nhánh --</option>');
        }
    };

    window.loadServiceCategories = async function loadServiceCategories() {
        const token = localStorage.getItem('authToken');
        const apiBaseUrl = window.API_BASE_URL || 'https://localhost:7173/api';
        const select = $('#serviceType');

        if (!select.length) {
            console.warn('⚠️ Service type select element not found yet (#serviceType), element may not be rendered');
            // Retry sau 500ms nếu element chưa có
            setTimeout(function() {
                if ($('#serviceType').length) {
                    console.log('🔄 Retrying loadServiceCategories after element found...');
                    loadServiceCategories();
                }
            }, 500);
            return;
        }

        console.log('🔄 Loading service categories... (token:', token ? 'present' : 'none', ')');
        select.prop('disabled', false);
        select.prop('required', true);
        
        // Chỉ update text nếu chưa có options (tránh xóa data đã load)
        if (select.find('option').length <= 1) {
            select.html('<option value="">-- Đang tải danh sách dịch vụ --</option>');
        }

        try {
            const headers = {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            };
            if (token) {
                headers['Authorization'] = `Bearer ${token}`;
            }

            console.log('📡 Fetching:', `${apiBaseUrl}/ServiceCategory`);
            const response = await fetch(`${apiBaseUrl}/ServiceCategory`, {
                method: 'GET',
                headers,
                mode: 'cors'
            });

            console.log('📥 Response status:', response.status);

            if (!response.ok) {
                const errorText = await response.text();
                console.error('❌ HTTP error:', response.status, errorText);
                throw new Error(`HTTP error! status: ${response.status}, message: ${errorText}`);
            }

            const result = await response.json();
            console.log('📦 Response data:', result);
            
            const categories = result.success && Array.isArray(result.data) ? result.data : (Array.isArray(result) ? result : []);
            console.log('✅ Categories found:', categories.length);

            select.empty();
            select.append('<option value="">-- Chọn dịch vụ --</option>');

            if (!categories.length) {
                console.warn('⚠️ No categories found');
                select.append('<option value="">-- Không có dịch vụ nào --</option>');
                return;
            }

            categories.forEach(category => {
                // Chỉ hiển thị tên dịch vụ, không hiển thị description
                select.append(`<option value="${category.id}">${category.name || 'N/A'}</option>`);
            });
            
            console.log('✅ Service categories loaded successfully');
        } catch (error) {
            console.error('❌ Error loading service categories:', error);
            select.empty();
            select.append('<option value="">-- Lỗi tải danh sách dịch vụ --</option>');
        }
    };

    async function loadUserCars() {
        const token = localStorage.getItem('authToken');
        // QUAN TRỌNG: Kiểm tra token không chỉ tồn tại mà còn phải hợp lệ
        if (!token || !isTokenValid(token)) {
            console.log('🚗 No valid token, skipping car load - user not logged in');
            // Không gọi ensureLicensePlateSelect() khi chưa đăng nhập
            // Để updateBookingFormForUser() xử lý việc hiển thị input text
            return;
        }

        let userId = null;
        try {
            const userInfoStr = localStorage.getItem('userInfo');
            if (userInfoStr) {
                const userInfo = JSON.parse(userInfoStr);
                userId = userInfo.userId || userInfo.id;
                console.log('🔍 UserId from localStorage:', userId);
            }
            if (!userId) {
                userId = getUserIdFromToken(token);
                console.log('🔍 UserId from token:', userId);
            }
        } catch (error) {
            console.error('❌ Error getting userId:', error);
            ensureLicensePlateSelect([]);
            return;
        }

        if (!userId) {
            console.warn('⚠️ No userId found, cannot load cars');
            ensureLicensePlateSelect([]);
            return;
        }

        try {
            const apiBaseUrl = window.API_BASE_URL || 'https://localhost:7173/api';
            // Load TẤT CẢ xe của user (không chỉ serviced) để hiển thị cả xe mới vừa thêm
            const url = `${apiBaseUrl}/CarOfAutoOwner/user/${userId}`;
            console.log(`🚗 Loading cars for user ${userId} from ${url}`);
            
            const response = await fetch(url, {
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json'
                }
            });

            console.log(`📥 Response status: ${response.status}`);

            if (!response.ok) {
                const errorText = await response.text();
                console.error(`❌ Failed to load user cars: ${response.status}`, errorText);
                // Vẫn hiển thị dropdown nhưng với message lỗi
                ensureLicensePlateSelect([]);
                return;
            }

            const result = await response.json();
            console.log('📦 Full response:', JSON.stringify(result, null, 2));
            
            // Parse response - có thể là { success: true, data: [...] } hoặc trực tiếp là array
            let cars = [];
            if (result && typeof result === 'object') {
                if (result.success && Array.isArray(result.data)) {
                    cars = result.data;
                } else if (Array.isArray(result)) {
                    cars = result;
                } else if (Array.isArray(result.data)) {
                    cars = result.data;
                }
            }
            
            console.log(`✅ Found ${cars.length} cars for user:`, cars);
            
            if (cars.length === 0) {
                console.warn('⚠️ No cars found for user. This might be normal if user has not registered any cars yet.');
            }

            ensureLicensePlateSelect(cars);
        } catch (error) {
            console.error('❌ Error loading serviced cars:', error);
            console.error('❌ Error details:', error.message, error.stack);
            // Vẫn hiển thị dropdown nhưng với message lỗi
            ensureLicensePlateSelect([]);
        }
    }

    function getDefaultBranchId() {
        try {
            const userInfoStr = localStorage.getItem('userInfo');
            if (userInfoStr) {
                const userInfo = JSON.parse(userInfoStr);
                if (userInfo.branchId) {
                    return parseInt(userInfo.branchId, 10);
                }
            }
        } catch (error) {
            console.warn('Could not get branch from user info:', error);
        }
        return 1; // fallback chi nhánh mặc định
    }

    // ----------------------------------------------------------
    // Form state helpers
    // ----------------------------------------------------------

    function updateBookingFormForUser() {
        const token = localStorage.getItem('authToken');
        // QUAN TRỌNG: Kiểm tra token không chỉ tồn tại mà còn phải hợp lệ (chưa hết hạn)
        const isLoggedIn = token && isTokenValid(token);

        console.log('🔄 updateBookingFormForUser - token exists:', !!token, 'isLoggedIn:', isLoggedIn);

        toggleGuestFields(isLoggedIn);

        // QUAN TRỌNG: Luôn reset và chuyển đổi đúng loại field dựa trên trạng thái đăng nhập
        const currentField = $('#licensePlate');
        
        if (isLoggedIn) {
            // User đã đăng nhập: hiển thị dropdown select
            console.log('✅ User logged in, showing license plate dropdown');
            // Nếu đang là input, chuyển sang select
            if (currentField.is('input')) {
                ensureLicensePlateSelect(); // Tạo select, sẽ được populate bởi loadUserCars()
            } else {
                // Đã là select, chỉ cần clear và để loadUserCars() populate
                currentField.empty();
            }
            $('#branch').prop('disabled', false);
            $('#mileage').val('');
            $('#message').val('');
        } else {
            // User chưa đăng nhập: hiển thị input text
            console.log('👤 User not logged in, showing license plate input text');
            // Nếu đang là select, chuyển sang input
            if (currentField.is('select')) {
                ensureLicensePlateInput(); // Tạo input text
            } else {
                // Đã là input, chỉ cần clear
                currentField.val('');
            }
            $('#mileage').val('');
            $('#message').val('');
            $('#serviceType').val('');
            $('#appointmentTime').val('');
            $('#branch').val('');
            $('#branch').prop('disabled', false);
        }
    }

    async function handleBookingSubmission() {
        const form = $('#bookingForm');
        const submitBtn = $('#submitBooking');

        // Validate required fields manually for better UX
        const tokenCheck = localStorage.getItem('authToken');
        const isLoggedInCheck = tokenCheck && isTokenValid(tokenCheck);
        
        // Validate guest fields if not logged in
        if (!isLoggedInCheck) {
            const fullName = $('#fullName').val().trim();
            if (!fullName || fullName.length < 2) {
                showAlert('Vui lòng nhập họ và tên (tối thiểu 2 ký tự)', 'warning');
                $('#fullName').focus();
                return;
            }
            
            const phone = $('#phone').val().trim();
            const phonePattern = /^(0[3|5|7|8|9])+([0-9]{8})$/;
            if (!phone || !phonePattern.test(phone)) {
                showAlert('Số điện thoại không hợp lệ. Vui lòng nhập số điện thoại 10 chữ số bắt đầu bằng 0 (VD: 0912345678)', 'warning');
                $('#phone').focus();
                return;
            }
        }
        
        // Validate license plate (when it's an input text)
        const licenseField = $('#licensePlate');
        if (licenseField.is('input')) {
            const licensePlate = licenseField.val().trim();
            if (!licensePlate) {
                showAlert('Vui lòng nhập biển số xe', 'warning');
                licenseField.focus();
                return;
            }
            // Validate license plate format: XX[A-Z]-XXXX or XX[A-Z]-XXXXX
            const licensePattern = /^[0-9]{2}[A-Z]{1,2}-[0-9]{4,5}$/;
            if (!licensePattern.test(licensePlate)) {
                showAlert('Biển số xe không hợp lệ. Vui lòng nhập đúng định dạng (VD: 30A-12345)', 'warning');
                licenseField.focus();
                return;
            }
        }

        if (!form[0].checkValidity()) {
            form[0].reportValidity();
            return;
        }

        // Lấy ngày, giờ, phút từ form (giống logic form của tư vấn viên)
        const dateStr = $('#appointmentDate').val();
        const hourStr = $('#appointmentHour').val();
        const minuteStr = $('#appointmentMinute').val();

        if (!dateStr || !hourStr || !minuteStr) {
            showAlert('Vui lòng chọn đầy đủ ngày, giờ và phút hẹn.', 'warning');
            return;
        }

        const [year, month, day] = dateStr.split('-').map(Number);
        const hour = parseInt(hourStr, 10);
        const minute = parseInt(minuteStr, 10);

        const appointmentTime = new Date(year, month - 1, day, hour, minute, 0);
        if (isNaN(appointmentTime.getTime())) {
            showAlert('Vui lòng chọn thời gian hợp lệ.', 'warning');
            return;
        }

        // Chuẩn hóa lại datetime theo múi giờ Việt Nam (GMT+7) ở dạng local string (không kèm múi giờ)
        const hourPadded = hour.toString().padStart(2, '0');
        const minutePadded = minute.toString().padStart(2, '0');
        const scheduledDateString = `${dateStr}T${hourPadded}:${minutePadded}:00`;

        // Yêu cầu: thời gian phải nằm trong tương lai
        const now = new Date();
        if (appointmentTime <= now) {
            showAlert('Vui lòng chọn thời gian trong tương lai.', 'warning');
            return;
        }

        // Giới hạn giờ làm việc: 7:00 - 17:45, phút: 00, 15, 30, 45 (giống form đặt lịch của tư vấn viên)
        const allowedMinutes = [0, 15, 30, 45];

        if (hour < 7 || hour > 17 || (hour === 17 && minute > 45)) {
            showAlert('Giờ làm việc: 7:00 - 17:45. Vui lòng chọn thời gian trong khung giờ này.', 'warning');
            return;
        }

        if (!allowedMinutes.includes(minute)) {
            showAlert('Vui lòng chọn phút là 00, 15, 30 hoặc 45 để phù hợp với giờ phục vụ.', 'warning');
            return;
        }
        
        // Validate mileage if provided
        const mileage = $('#mileage').val();
        if (mileage) {
            const mileageNum = parseInt(mileage, 10);
            if (isNaN(mileageNum) || mileageNum < 0 || mileageNum > 9999999) {
                showAlert('Số km không hợp lệ. Vui lòng nhập số từ 0 đến 9,999,999', 'warning');
                $('#mileage').focus();
                return;
            }
        }

        const token = localStorage.getItem('authToken');
        const isLoggedIn = !!token;
        const apiBaseUrl = window.API_BASE_URL || 'https://localhost:7173/api';

        let bookingData;
        let endpoint;
        const headers = { 'Content-Type': 'application/json' };

        if (isLoggedIn) {
            let userId = null;
            try {
                const userInfoStr = localStorage.getItem('userInfo');
                if (userInfoStr) {
                    const userInfo = JSON.parse(userInfoStr);
                    userId = userInfo.userId || userInfo.id;
                }
                if (!userId) {
                    userId = getUserIdFromToken(token);
                }
            } catch (error) {
                console.error('Error getting userId:', error);
                showAlert('Không thể xác định người dùng. Vui lòng đăng nhập lại.', 'danger');
                return;
            }

            const licenseField = $('#licensePlate');
            let carId = null;
            let usePublicFlow = false;

            if (licenseField.is('select')) {
                if (licenseField.prop('disabled')) {
                    showAlert('Bạn chưa có xe nào đã bảo dưỡng. Vui lòng liên hệ tư vấn để thêm xe trước khi đặt lịch.', 'warning');
                    return;
                }
                carId = licenseField.val();
                if (!carId) {
                    showAlert('Vui lòng chọn biển số xe', 'warning');
                    return;
                }
            } else {
                usePublicFlow = true;
            }

            if (!usePublicFlow) {
                const branchValue = $('#branch').val();
                if (!branchValue) {
                    showAlert('Vui lòng chọn chi nhánh tiếp nhận yêu cầu.', 'warning');
                    return;
                }

                const branchId = parseInt(branchValue, 10) || getDefaultBranchId();
                const serviceTypeValue = $('#serviceType').val();
                bookingData = {
                    userId: parseInt(userId, 10),
                    carId: parseInt(carId, 10),
                    scheduledDate: scheduledDateString,
                    branchId: branchId,
                    statusCode: 'PENDING'
                };
                
                // Thêm ServiceCategoryId nếu có
                if (serviceTypeValue) {
                    const serviceId = parseInt(serviceTypeValue, 10);
                    if (!isNaN(serviceId)) {
                        bookingData.serviceCategoryId = serviceId;
                    }
                }
                
                endpoint = `${apiBaseUrl}/ServiceSchedule`;
                headers['Authorization'] = `Bearer ${token}`;
            } else {
                // Không có xe đã bảo dưỡng -> fallback sang public booking flow
                bookingData = buildPublicBookingPayload(appointmentTime, scheduledDateString);
                endpoint = `${apiBaseUrl}/ServiceSchedule/public-booking`;
            }
        } else {
            bookingData = buildPublicBookingPayload(appointmentTime, scheduledDateString);
            endpoint = `${apiBaseUrl}/ServiceSchedule/public-booking`;
        }

        const originalText = submitBtn.html();
        submitBtn.html('<i class="fas fa-spinner fa-spin me-2"></i>Đang xử lý...');
        submitBtn.prop('disabled', true);

        try {
            const response = await fetch(endpoint, {
                method: 'POST',
                headers,
                body: JSON.stringify(bookingData)
            });

            const result = await response.json();
            if (result.success) {
                // Luôn hiển thị thông báo tiếng Việt cho người dùng, không dùng message tiếng Anh từ API
                const successMessage = 'Đặt lịch thành công! Chúng tôi sẽ liên hệ lại với bạn sớm nhất.';
                showAlert(successMessage, 'success');
                $('#bookingModal').modal('hide');
                form[0].reset();
                updateBookingFormForUser();
            } else {
                showAlert(result.message || 'Có lỗi xảy ra khi đặt lịch. Vui lòng thử lại sau.', 'danger');
            }
        } catch (error) {
            console.error('Booking error:', error);
            showAlert('Có lỗi xảy ra khi đặt lịch. Vui lòng thử lại sau.', 'danger');
        } finally {
            submitBtn.html(originalText);
            submitBtn.prop('disabled', false);
        }
    }

    function buildPublicBookingPayload(appointmentTime, scheduledDateString) {
        const serviceTypeValue = $('#serviceType').val();
        const payload = {
            fullName: $('#fullName').val(),
            email: $('#email').val() || null,
            phone: $('#phone').val(),
            carName: $('#vehicleType').val() || 'Chưa xác định',
            licensePlate: $('#licensePlate').val() || null,
            carModel: $('#vehicleType').val() || null,
            mileage: $('#mileage').val() ? parseInt($('#mileage').val(), 10) : null,
            // Gửi dạng local datetime (không convert sang UTC) để server hiểu là giờ Việt Nam
            scheduledDate: scheduledDateString,
            branchId: parseInt($('#branch').val() || getDefaultBranchId(), 10),
            message: $('#message').val() || null
        };
        
        // Nếu serviceType là số (ID từ database), thêm vào ServiceCategoryId
        // Nếu là string (giá trị cũ), giữ lại serviceType
        if (serviceTypeValue) {
            const serviceId = parseInt(serviceTypeValue, 10);
            if (!isNaN(serviceId)) {
                payload.serviceCategoryId = serviceId;
            } else {
                payload.serviceType = serviceTypeValue;
            }
        }
        
        return payload;
    }

    // ----------------------------------------------------------
    // Event wiring
    // ----------------------------------------------------------

    // Load branches và service categories ngay khi page load (không cần đợi modal mở)
    $(document).ready(function () {
        // Pre-load branches và service categories để sẵn sàng khi modal mở
        console.log('📋 Booking form script loaded, pre-loading data...');
        
        // Thử load ngay lập tức (element có thể chưa render, nhưng sẽ retry khi modal mở)
        setTimeout(function() {
            loadBranches();
            loadServiceCategories();
        }, 500); // Delay nhỏ để đảm bảo DOM đã render
        
        $('#bookingModal').on('show.bs.modal', function () {
            console.log('📋 Modal opening, FORCE RESETTING form...');
            
            // QUAN TRỌNG: Force reset form và tất cả fields
            $('#bookingForm')[0].reset();
            
            // Clear tất cả input values
            $('#serviceType').val('');
            $('#appointmentDate').val('');
            $('#appointmentHour').val('');
            $('#appointmentMinute').val('');
            $('#branch').val('');
            $('#mileage').val('');
            $('#message').val('');
            
            // QUAN TRỌNG: Cập nhật form state (ẩn/hiện các trường) dựa trên trạng thái đăng nhập
            updateBookingFormForUser();
            
            // QUAN TRỌNG: Kiểm tra lại token và reset field type
            const token = localStorage.getItem('authToken');
            const isValidToken = token && isTokenValid(token);
            
            console.log('🔍 Token check - exists:', !!token, 'valid:', isValidToken);
            
            // Force reset field type dựa trên trạng thái đăng nhập
            if (isValidToken) {
                console.log('✅ User is logged in, converting to select dropdown');
                // Chuyển sang select dropdown
                ensureLicensePlateSelect();
                $('#branch').prop('disabled', false);
            } else {
                console.log('👤 User NOT logged in, converting to input text');
                // Chuyển sang input text
                ensureLicensePlateInput();
                $('#branch').prop('disabled', false);
            }
            
            // Luôn reload để đảm bảo data mới nhất
            loadBranches();
            loadServiceCategories();
            
            // Chỉ load user cars nếu đã đăng nhập và token hợp lệ
            if (isValidToken) {
                console.log('🔄 Loading user cars...');
                loadUserCars();
            } else {
                console.log('🚗 User not logged in, skipping car load');
            }

            const tomorrow = new Date();
            tomorrow.setDate(tomorrow.getDate() + 1);
            const minDateStr = tomorrow.toISOString().split('T')[0];
            $('#appointmentDate').attr('min', minDateStr);

            // Populate giờ làm việc từ 7 đến 17 (giống tư vấn viên)
            const hourSelect = $('#appointmentHour');
            hourSelect.empty().append('<option value=\"\">Giờ</option>');
            for (let hour = 7; hour <= 17; hour++) {
                const hourStr = hour.toString().padStart(2, '0');
                hourSelect.append(`<option value=\"${hourStr}\">${hourStr}</option>`);
            }

            // Populate phút: 00, 15, 30, 45
            const minuteSelect = $('#appointmentMinute');
            const minutes = ['00', '15', '30', '45'];
            minuteSelect.empty().append('<option value=\"\">Phút</option>');
            minutes.forEach(minute => {
                minuteSelect.append(`<option value=\"${minute}\">${minute}</option>`);
            });
        });

        $('#bookingModal').on('shown.bs.modal', function () {
            console.log('📋 Modal shown, double-checking form state...');
            
            // Double-check trạng thái đăng nhập và đảm bảo form state đúng
            updateBookingFormForUser();
            
            // Double-check trạng thái đăng nhập
            const token = localStorage.getItem('authToken');
            const isValidToken = token && isTokenValid(token);
            const currentField = $('#licensePlate');
            
            // Đảm bảo field type đúng
            if (isValidToken) {
                if (currentField.is('input')) {
                    console.log('⚠️ Field is input but user is logged in, converting to select');
                    ensureLicensePlateSelect();
                    loadUserCars();
                }
            } else {
                if (currentField.is('select')) {
                    console.log('⚠️ Field is select but user is not logged in, converting to input');
                    ensureLicensePlateInput();
                }
            }
            
            // Đảm bảo load branches nếu chưa có
            if ($('#branch option').length <= 1) {
                console.log('🔄 Reloading branches...');
                loadBranches();
            }
            
            // Đảm bảo load service categories nếu chưa có
            if ($('#serviceType option').length <= 1) {
                console.log('🔄 Reloading service categories...');
                loadServiceCategories();
            }
            
            // Chỉ load user cars nếu đã đăng nhập và token hợp lệ
            if (isValidToken) {
                console.log('🔄 Loading user cars...');
                loadUserCars();
            }

            // Đảm bảo dropdown giờ/phút luôn có dữ liệu
            if ($('#appointmentHour option').length <= 1) {
                const hourSelect = $('#appointmentHour');
                hourSelect.empty().append('<option value=\"\">Giờ</option>');
                for (let hour = 7; hour <= 17; hour++) {
                    const hourStr = hour.toString().padStart(2, '0');
                    hourSelect.append(`<option value=\"${hourStr}\">${hourStr}</option>`);
                }
            }

            if ($('#appointmentMinute option').length <= 1) {
                const minuteSelect = $('#appointmentMinute');
                const minutes = ['00', '15', '30', '45'];
                minuteSelect.empty().append('<option value=\"\">Phút</option>');
                minutes.forEach(minute => {
                    minuteSelect.append(`<option value=\"${minute}\">${minute}</option>`);
                });
            }
        });

        $('#bookingModal').on('hidden.bs.modal', function () {
            console.log('📋 Modal closed, FORCE RESETTING to default state...');
            // Reset form về trạng thái ban đầu
            $('#bookingForm')[0].reset();
            
            // Clear tất cả values
            $('#serviceType').val('');
            $('#appointmentDate').val('');
            $('#appointmentHour').val('');
            $('#appointmentMinute').val('');
            $('#branch').val('');
            $('#mileage').val('');
            $('#message').val('');
            
            // LUÔN reset về input text khi đóng modal (sẽ được set lại đúng khi mở)
            ensureLicensePlateInput();
        });

        $('#submitBooking').on('click', function (e) {
            e.preventDefault();
            handleBookingSubmission();
        });

        $('#bookingForm').on('submit', function (e) {
            e.preventDefault();
            handleBookingSubmission();
        });

        $(document).on('click', '[data-bs-target="#bookingModal"], .floating-tab-item[data-action="booking"]', function () {
            console.log('📋 Booking button clicked, resetting form and ensuring data is loaded...');
            
            // Reset form trước
            $('#bookingForm')[0].reset();
            
            // QUAN TRỌNG: updateBookingFormForUser() phải chạy TRƯỚC để set đúng loại field
            updateBookingFormForUser();
            
            // Đảm bảo load branches nếu chưa có
            if ($('#branch option').length <= 1) {
                loadBranches();
            }
            
            // Đảm bảo load service categories nếu chưa có
            if ($('#serviceType option').length <= 1) {
                loadServiceCategories();
            }
            
            // Chỉ load user cars nếu đã đăng nhập và token hợp lệ
            const token = localStorage.getItem('authToken');
            if (token && isTokenValid(token)) {
                console.log('🔄 Loading user cars from button click...');
                loadUserCars();
            } else {
                console.log('🚗 User not logged in, ensuring input text field');
                ensureLicensePlateInput();
            }
        });
    });

    // Open Add Car Modal from Booking Form
    function openAddCarModalFromBooking() {
        // Reset form
        document.getElementById('addCarFormBooking').reset();
        
        // Show modal
        const modal = new bootstrap.Modal(document.getElementById('addCarModalBooking'));
        modal.show();
    }

    // Submit Add Car from Booking Form
    async function submitAddCarFromBooking() {
        const form = document.getElementById('addCarFormBooking');
        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }

        const token = localStorage.getItem('authToken');
        if (!token || !isTokenValid(token)) {
            showAlert('Vui lòng đăng nhập để thêm xe', 'warning');
            return;
        }

        const userInfoStr = localStorage.getItem('userInfo');
        if (!userInfoStr) {
            showAlert('Vui lòng đăng nhập để thêm xe', 'warning');
            return;
        }

        const userInfo = JSON.parse(userInfoStr);
        const userId = userInfo.userId || userInfo.id;

        if (!userId) {
            // Try to get from token
            userId = getUserIdFromToken(token);
        }

        if (!userId) {
            showAlert('Không thể xác định người dùng. Vui lòng đăng nhập lại.', 'danger');
            return;
        }

        const carData = {
            userId: parseInt(userId),
            carName: document.getElementById('newCarNameBooking').value.trim(),
            licensePlate: document.getElementById('newLicensePlateBooking').value.trim()
        };

        // Validate required fields
        if (!carData.carName || !carData.licensePlate) {
            showAlert('Vui lòng điền đầy đủ thông tin bắt buộc (Tên xe và Biển số)', 'warning');
            return;
        }

        // Validate license plate format
        const licensePlatePattern = /^[0-9]{2}[A-Z]{1,2}-[0-9]{4,5}$/;
        if (!licensePlatePattern.test(carData.licensePlate)) {
            showAlert('Biển số xe không hợp lệ. Vui lòng nhập đúng định dạng (VD: 30A-12345)', 'warning');
            return;
        }

        const submitBtn = document.getElementById('submitAddCarBookingBtn');
        const originalText = submitBtn.innerHTML;
        submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Đang lưu...';
        submitBtn.disabled = true;

        try {
            const apiBaseUrl = window.API_BASE_URL || 'https://localhost:7173/api';
            const response = await fetch(`${apiBaseUrl}/CarOfAutoOwner`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(carData)
            });

            const result = await response.json();

            if (response.ok && result.success !== false) {
                showAlert('Thêm xe thành công!', 'success');
                
                // Close modal
                const modal = bootstrap.Modal.getInstance(document.getElementById('addCarModalBooking'));
                modal.hide();
                
                // Reset dropdown selection
                $('#licensePlate').val('');
                
                // Reload cars list
                loadUserCars();
            } else {
                const errorMessage = result.message || 'Không thể thêm xe. Vui lòng thử lại.';
                showAlert(errorMessage, 'danger');
            }
        } catch (error) {
            console.error('Error adding car:', error);
            showAlert('Có lỗi xảy ra khi thêm xe. Vui lòng thử lại sau.', 'danger');
        } finally {
            submitBtn.innerHTML = originalText;
            submitBtn.disabled = false;
        }
    }

    // Attach submit handler for add car form
    $(document).ready(function() {
        const submitBtn = document.getElementById('submitAddCarBookingBtn');
        if (submitBtn) {
            submitBtn.addEventListener('click', submitAddCarFromBooking);
        }
    });

    // Expose helpers for debugging if needed
    window.updateBookingFormForUser = updateBookingFormForUser;
    window.loadUserCars = loadUserCars;
    window.loadServiceCategories = loadServiceCategories;
    window.openAddCarModalFromBooking = openAddCarModalFromBooking;

})();



