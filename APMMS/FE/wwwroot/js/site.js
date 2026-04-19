  // APMMS - Auto Parts Management & Maintenance System JavaScript

// NOTE: This file was accidentally cleared. The contents below rebuild the core UI
// interactions and authentication flows that other pages depend on.

// ---------------------------------------------------------------------------
// Global Authentication Error Handler - Xử lý token hết hạn
// ---------------------------------------------------------------------------

// Biến để tránh xử lý nhiều lần khi có nhiều request 401 cùng lúc
let isHandlingExpiredToken = false;

// Lưu fetch gốc TRƯỚC KHI override để sử dụng trong các hàm helper
const originalFetch = window.fetch;

/**
 * Xử lý khi token hết hạn - tự động đăng xuất và chuyển về trang đăng nhập
 */
function handleExpiredToken(message) {
    // Tránh xử lý nhiều lần
    if (isHandlingExpiredToken) {
        return;
    }
    isHandlingExpiredToken = true;

    console.warn('Token đã hết hạn, đang đăng xuất...');

    // Xóa tất cả thông tin đăng nhập
    localStorage.removeItem('authToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('isLoggedIn');
    localStorage.removeItem('userInfo');
    localStorage.removeItem('sidebarMenuStates'); // Xóa trạng thái sidebar

    // Hiển thị thông báo
    const alertMessage = message || 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.';
    showAlert(alertMessage, 'warning');

    // Cập nhật UI
    showLoginButton();

    // Đóng dropdown nếu có
    try {
        const dropdown = bootstrap.Dropdown.getInstance(document.getElementById('profileDropdownToggle'));
        if (dropdown) {
            dropdown.hide();
        }
    } catch (err) {
        console.warn('Không thể đóng dropdown profile:', err);
    }

    // Chuyển về trang chủ sau 2 giây
    setTimeout(() => {
        // Kiểm tra xem có đang ở trang login không
        const currentPath = window.location.pathname.toLowerCase();
        if (currentPath !== '/auth/login' && currentPath !== '/') {
            window.location.href = '/';
        } else {
            // Nếu đã ở trang chủ, chỉ reload để cập nhật UI
            window.location.reload();
        }
        isHandlingExpiredToken = false;
    }, 2000);
}

/**
 * Refresh access token bằng refresh token
 */
let isRefreshing = false;
let refreshPromise = null;

async function refreshAccessToken() {
    // Tránh refresh nhiều lần cùng lúc
    if (isRefreshing && refreshPromise) {
        return refreshPromise;
    }

    isRefreshing = true;
    refreshPromise = (async () => {
        try {
            const refreshToken = localStorage.getItem('refreshToken');
            if (!refreshToken) {
                throw new Error('Không có refresh token');
            }

            // Gọi API refresh token
            const response = await originalFetch('/api/Auth/refresh', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ token: refreshToken })
            });

            if (!response.ok) {
                throw new Error('Refresh token thất bại');
            }

            const result = await response.json();
            
            if (result.success && result.data) {
                // Lưu token mới
                localStorage.setItem('authToken', result.data.token);
                if (result.data.refreshToken) {
                    localStorage.setItem('refreshToken', result.data.refreshToken);
                }
                
                console.log('Token đã được refresh thành công');
                return result.data.token;
            } else {
                throw new Error(result.message || 'Refresh token thất bại');
            }
        } catch (error) {
            console.error('Refresh token error:', error);
            // Refresh thất bại, đăng xuất
            handleExpiredToken('Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.');
            throw error;
        } finally {
            isRefreshing = false;
            refreshPromise = null;
        }
    })();

    return refreshPromise;
}

/**
 * Wrapper cho fetch API để tự động thêm token và xử lý 401
 * Tự động refresh token khi access token hết hạn
 */
async function authenticatedFetch(url, options = {}) {
    // Lấy token từ localStorage
    let token = localStorage.getItem('authToken');

    // Tạo headers mới hoặc sử dụng headers có sẵn
    const headers = new Headers(options.headers || {});

    // Thêm token vào header nếu có
    if (token) {
        headers.set('Authorization', `Bearer ${token}`);
    }

    // Thêm Content-Type nếu chưa có
    if (!headers.has('Content-Type') && options.body && typeof options.body === 'string') {
        headers.set('Content-Type', 'application/json');
    }

    // Merge options
    const fetchOptions = {
        ...options,
        headers: headers
    };

    try {
        // Sử dụng originalFetch để tránh vòng lặp vô hạn
        let response = await originalFetch(url, fetchOptions);

        // Xử lý 401 Unauthorized - Token hết hạn hoặc không hợp lệ
        if (response.status === 401) {
            // Không xử lý 401 cho các endpoint login/auth để tránh vòng lặp
            const urlLower = url.toLowerCase();
            if (!urlLower.includes('/auth/login') && 
                !urlLower.includes('/auth/refresh') &&
                !urlLower.includes('/auth/validate') &&
                !urlLower.includes('/auth/forgot-password') &&
                !urlLower.includes('/auth/reset-password')) {
                
                // Thử refresh token trước
                try {
                    const newToken = await refreshAccessToken();
                    
                    // Retry request với token mới
                    headers.set('Authorization', `Bearer ${newToken}`);
                    const retryOptions = {
                        ...options,
                        headers: headers
                    };
                    
                    response = await originalFetch(url, retryOptions);
                    
                    // Nếu vẫn 401 sau khi refresh, thì đăng xuất
                    if (response.status === 401) {
                        handleExpiredToken('Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.');
                        return {
                            ...response,
                            isExpired: true
                        };
                    }
                    
                    // Refresh thành công, trả về response mới
                    return response;
                } catch (refreshError) {
                    // Refresh thất bại, đã được xử lý trong refreshAccessToken
                    return {
                        ...response,
                        isExpired: true,
                        errorMessage: 'Không thể làm mới token. Vui lòng đăng nhập lại.'
                    };
                }
            }
        }

        return response;
    } catch (error) {
        console.error('Fetch error:', error);
        throw error;
    }
}

// Override fetch global để tự động xử lý authentication
window.fetch = async function(url, options = {}) {
    // Chỉ xử lý cho các request đến API backend (không phải static files)
    const urlString = typeof url === 'string' ? url : (url instanceof Request ? url.url : '');
    
    // Bỏ qua các request đến static files, images, CSS, JS
    if (urlString.match(/\.(jpg|jpeg|png|gif|svg|css|js|woff|woff2|ttf|eot|ico|map)$/i)) {
        return originalFetch(url, options);
    }

    // Bỏ qua các request đến cùng origin (FE server) trừ API calls
    if (urlString.startsWith('/') && !urlString.startsWith('/api/')) {
        // Nếu là request đến FE server và không phải API, dùng fetch gốc
        // Nhưng vẫn kiểm tra 401
        try {
            const response = await originalFetch(url, options);
            
            // Xử lý 401 cho các request đến FE server
            if (response.status === 401) {
                const urlLower = urlString.toLowerCase();
                if (!urlLower.includes('/auth/login') && 
                    !urlLower.includes('/auth/validate') &&
                    !urlLower.includes('/auth/forgot-password') &&
                    !urlLower.includes('/auth/reset-password')) {
                    handleExpiredToken();
                }
            }
            
            return response;
        } catch (error) {
            throw error;
        }
    }

    // Sử dụng authenticatedFetch cho các API calls
    return authenticatedFetch(url, options);
};

// Export functions để sử dụng ở nơi khác
window.handleExpiredToken = handleExpiredToken;
window.authenticatedFetch = authenticatedFetch;

$(document).ready(function () {
    // Sidebar toggle functionality
    $('#sidebarCollapse').on('click', function () {
        $('#sidebar').toggleClass('active');
        $('#content').toggleClass('active');
    });

    // Auto-hide sidebar on mobile
    $(window).on('resize', function () {
        if ($(window).width() <= 768) {
            $('#sidebar').removeClass('active');
            $('#content').removeClass('active');
        }
    });

    // Smooth scrolling for anchor links
    $('a[href^="#"]').on('click', function (event) {
        const href = $(this).attr('href');
        if (!href || href === '#') {
            return;
        }

        const target = $(href);
        if (target.length) {
            event.preventDefault();
            $('html, body').stop().animate({
                scrollTop: target.offset().top - 100
            }, 1000);
        }
    });

    // Initialize tooltips & popovers
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    var popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
    popoverTriggerList.map(function (popoverTriggerEl) {
        return new bootstrap.Popover(popoverTriggerEl);
    });

    // Dashboard animations
    $('.dashboard-card').each(function (index) {
        $(this).css('animation-delay', (index * 0.1) + 's');
    });

    // Table row click handlers
    $('.table-modern tbody tr').on('click', function () {
        $(this).addClass('table-active').siblings().removeClass('table-active');
    });

    // Search filtering
    $('#searchInput').on('keyup', function () {
        var value = $(this).val().toLowerCase();
        $('.table-modern tbody tr').filter(function () {
            $(this).toggle($(this).text().toLowerCase().indexOf(value) > -1);
        });
    });

    // Notifications
    $('.notification-item').on('click', function () {
        $(this).removeClass('bg-light').addClass('bg-white');
    });

    // Global form validation helper
    $('.needs-validation').on('submit', function (event) {
        if (this.checkValidity() === false) {
            event.preventDefault();
            event.stopPropagation();
        }
        $(this).addClass('was-validated');
    });

    // Simple loading demonstration for buttons
    $('.btn-loading').on('click', function () {
        var btn = $(this);
        var originalText = btn.html();
        btn.html('<i class="fas fa-spinner fa-spin"></i> Đang xử lý...');
        btn.prop('disabled', true);

        setTimeout(function () {
            btn.html(originalText);
            btn.prop('disabled', false);
        }, 2000);
    });

    // Modal content loader
    $('.modal').on('show.bs.modal', function (event) {
        var button = $(event.relatedTarget);
        var modal = $(this);

        if (button && button.data('load')) {
            var url = button.data('load');
            modal.find('.modal-body').load(url);
        }
    });

    console.log('Initializing authentication...');
    initializeAuth();
    console.log('APMMS Frontend initialized successfully');
    initializeFloatingTab();
});

// ---------------------------------------------------------------------------
// Authentication helpers
// ---------------------------------------------------------------------------

function initializeAuth() {
    checkLoginStatus();

    $('.login-form').on('submit', function (e) {
        e.preventDefault();
        e.stopPropagation();

        if ($(this).find('#username').length > 0) {
            handleLogin();
        }
        return false;
    });

    $('#logoutBtn').on('click', function (e) {
        e.preventDefault();
        handleLogout();
    });

    // Profile link should work normally - Bootstrap dropdown will handle closing
    // No special handling needed for Profile navigation
}

async function checkLoginStatus() {
    const token = localStorage.getItem('authToken');

    if (!token) {
        showLoginButton();
        return;
    }

    try {
        const response = await authenticatedFetch('/Auth/GetUserInfo', {
            method: 'GET'
        });

        // Kiểm tra nếu token đã hết hạn
        if (response.isExpired) {
            return;
        }

        const result = await response.json();

        if (result.isLoggedIn) {
            // ✅ Lấy firstName/lastName từ response
            const firstName = result.firstName || null;
            const lastName = result.lastName || null;
            let fullName = result.fullName;
            if (!fullName && firstName && lastName) {
                fullName = `${firstName} ${lastName}`.trim();
            } else if (!fullName) {
                fullName = result.username;
            }
            
            const userInfo = {
                username: result.username,
                firstName: firstName,
                lastName: lastName,
                fullName: fullName,
                email: result.email || 'user@example.com',
                role: result.role || getRoleName(result.roleId),
                roleId: result.roleId || 0,
                userId: result.userId || result.id || null,
                id: result.userId || result.id || null,
                branchId: result.branchId || result.BranchId || null
            };
            localStorage.setItem('userInfo', JSON.stringify(userInfo));
            localStorage.setItem('isLoggedIn', 'true');
            showProfileDropdown(userInfo);
        } else {
            showLoginButton();
        }
    } catch (error) {
        console.error('Check login status error:', error);
        showLoginButton();
    }
}

async function handleLogin() {
    const username = $('#username').val();
    const password = $('#password').val();

    if (!username || !password) {
        showAlert('Vui lòng nhập đầy đủ thông tin đăng nhập', 'warning');
        return;
    }

    const loginBtn = $('.btn-login');
    const originalText = loginBtn.html();
    loginBtn.html('<i class="fas fa-spinner fa-spin"></i> Đang đăng nhập...');
    loginBtn.prop('disabled', true);

    try {
        // Sử dụng originalFetch cho login để tránh vòng lặp
        const response = await originalFetch('/Auth/Login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, password })
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const result = await response.json();

        if (result.success) {
            let userId = result.userId;
            if (!userId && result.token) {
                userId = decodeUserIdFromToken(result.token);
            }

            // Reset sidebar state khi đăng nhập mới (mỗi session mới bắt đầu với sidebar mặc định)
            localStorage.removeItem('sidebarMenuStates');

            // Lưu access token và refresh token
            localStorage.setItem('authToken', result.token);
            if (result.refreshToken) {
                localStorage.setItem('refreshToken', result.refreshToken);
            }
            localStorage.setItem('isLoggedIn', 'true');
            
            // Lấy branchId từ result hoặc decode từ token
            let branchId = result.branchId || result.BranchId;
            if (!branchId && result.token) {
                try {
                    const payload = JSON.parse(atob(result.token.split('.')[1]));
                    branchId = payload['BranchId'] || payload['branchId'] || payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/branchid'];
                } catch (e) {
                    console.warn('Could not get branchId from token:', e);
                }
            }
            
            // ✅ Lấy firstName/lastName từ response
            const firstName = result.firstName || null;
            const lastName = result.lastName || null;
            let fullName = result.fullName;
            if (!fullName && firstName && lastName) {
                fullName = `${firstName} ${lastName}`.trim();
            } else if (!fullName) {
                fullName = username;
            }
            
            localStorage.setItem('userInfo', JSON.stringify({
                username,
                firstName: firstName,
                lastName: lastName,
                fullName: fullName,
                email: result.email || 'user@example.com',
                role: getRoleName(result.roleId),
                roleId: result.roleId,
                userId,
                id: userId,
                branchId: branchId || null,
                token: result.token
            }));

            $('#loginModal').modal('hide');
            showProfileDropdown({
                username,
                firstName: firstName,
                lastName: lastName,
                fullName: fullName,
                email: result.email || 'user@example.com',
                role: getRoleName(result.roleId)
            });
            showAlert('Đăng nhập thành công!', 'success');

            if (result.redirectTo && result.redirectTo !== '/') {
                setTimeout(() => {
                    // Dùng replace thay vì href để không đẩy trang chủ vào history
                    // Khi bấm nút Back, sẽ không quay về trang chủ
                    window.location.replace(result.redirectTo);
                }, 1500);
            }
        } else {
            showAlert(result.error || 'Đăng nhập thất bại', 'danger');
        }
    } catch (error) {
        console.error('Login error:', error);
        showAlert('Lỗi kết nối đến server', 'danger');
    } finally {
        loginBtn.html(originalText);
        loginBtn.prop('disabled', false);
    }
}

function decodeUserIdFromToken(token) {
    if (!token) return null;
    try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        return payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ||
               payload['UserId'] ||
               payload['nameid'] ||
               payload['sub'];
    } catch (error) {
        console.error('Error decoding token:', error);
        return null;
    }
}

async function handleLogout() {
    try {
        await authenticatedFetch('/Auth/Logout', {
            method: 'POST'
        });
    } catch (error) {
        console.error('Logout error:', error);
    } finally {
        localStorage.removeItem('authToken');
        localStorage.removeItem('refreshToken');
        localStorage.removeItem('isLoggedIn');
        localStorage.removeItem('userInfo');
        localStorage.removeItem('sidebarMenuStates'); // Xóa trạng thái sidebar

        showLoginButton();
        showAlert('Đã đăng xuất', 'info');

        // Đảm bảo đóng dropdown và refresh giao diện
        try {
            const dropdown = bootstrap.Dropdown.getInstance(document.getElementById('profileDropdownToggle'));
            if (dropdown) {
                dropdown.hide();
            }
        } catch (err) {
            console.warn('Không thể đóng dropdown profile:', err);
        }

        // Reload trang để đồng bộ trạng thái giữa các layout
        setTimeout(() => {
            window.location.href = '/';
        }, 300);
    }
}

async function showProfileDropdown(userInfo) {
    $('#loginBtn').hide();
    $('#profileDropdown').show();
    
    // Hiển thị tạm thời
    let displayName = userInfo.fullName || userInfo.username || 'Người dùng';
    $('#profileName').text(displayName);
    
    // ✅ Tự động gọi API BE để lấy firstName/lastName
    let userId = userInfo.userId || userInfo.id;
    
    // Clean and validate userId - remove any non-numeric characters
    if (userId) {
        const idStr = String(userId).replace(/[^0-9]/g, '');
        userId = parseInt(idStr, 10);
        if (isNaN(userId) || userId <= 0) {
            console.error('Invalid user ID:', userInfo.userId || userInfo.id);
            userId = null;
        }
    }
    
    if (userId) {
        try {
            const apiBaseUrl = window.API_BASE_URL || 'https://localhost:7173/api';
            const token = localStorage.getItem('authToken');
            
            if (token) {
                const response = await fetch(`${apiBaseUrl}/AutoOwner/${userId}`, {
                    headers: {
                        'Authorization': `Bearer ${token}`,
                        'Content-Type': 'application/json'
                    }
                });
                
                if (response.ok) {
                    const userData = await response.json();
                    const firstName = userData.firstName || userData.FirstName;
                    const lastName = userData.lastName || userData.LastName;
                    
                    // Tạo displayName từ firstName + lastName
                    if (firstName && lastName) {
                        displayName = `${firstName} ${lastName}`.trim();
                    } else if (firstName) {
                        displayName = firstName;
                    } else if (lastName) {
                        displayName = lastName;
                    }
                    
                    // Cập nhật hiển thị
                    $('#profileName').text(displayName);
                    
                    // Cập nhật localStorage
                    const updatedUserInfo = {
                        ...userInfo,
                        firstName: firstName,
                        lastName: lastName,
                        fullName: displayName
                    };
                    localStorage.setItem('userInfo', JSON.stringify(updatedUserInfo));
                }
            }
        } catch (error) {
            console.warn('Không thể lấy thông tin họ tên từ API:', error);
        }
    } else if (userInfo.firstName && userInfo.lastName) {
        // Nếu đã có firstName/lastName trong userInfo, dùng luôn
        displayName = `${userInfo.firstName} ${userInfo.lastName}`.trim();
        $('#profileName').text(displayName);
    }
}

function showLoginButton() {
    $('#profileDropdown').hide();
    $('#loginBtn').show();
}

function showAlert(message, type = 'info') {
    const alertHtml = `
        <div class="alert alert-${type} alert-dismissible fade show position-fixed"
             style="top: 20px; right: 20px; z-index: 9999; min-width: 300px;" role="alert">
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    `;

    $('body').append(alertHtml);
    setTimeout(() => {
        $('.alert').alert('close');
    }, 3000);
}

function getRoleName(roleId) {
    switch (roleId) {
        case 1: return 'Admin';
        case 2: return 'Branch Manager';
        case 3: return 'Accountant';
        case 4: return 'Technician';
        case 5: return 'Warehouse Keeper';
        case 6: return 'Consulter';
        case 7: return 'Auto Owner';
        case 8: return 'Guest';
        default: return 'User';
    }
}

// Hàm chuyển đổi tên chức vụ sang tiếng Việt
function getRoleNameInVietnamese(roleName) {
    if (!roleName) return '';
    
    const roleMap = {
        'Admin': 'Tổng giám đốc',
        'Branch Manager': 'Giám đốc chi nhánh',
        'Accountant': 'Kế toán',
        'Technician': 'Kỹ thuật viên',
        'Warehouse Keeper': 'Thủ kho',
        'Consulter': 'Tư vấn viên',
        'Auto Owner': 'Chủ xe',
        'Guest': 'Khách'
    };
    
    return roleMap[roleName] || roleName;
}

function showLoginModal() {
    $('#loginModal').modal('show');
}

// ---------------------------------------------------------------------------
// Floating tab shortcuts (booking/contact)
// ---------------------------------------------------------------------------

function initializeFloatingTab() {
    $('.floating-tab-toggle').on('click', function () {
        $('.floating-tab').toggleClass('collapsed');
        const arrow = $(this).find('i');
        if ($('.floating-tab').hasClass('collapsed')) {
            arrow.removeClass('fa-chevron-left').addClass('fa-chevron-right');
        } else {
            arrow.removeClass('fa-chevron-right').addClass('fa-chevron-left');
        }
    });

    $('.floating-tab-item[data-action="booking"]').on('click', function (e) {
        e.preventDefault();
        e.stopPropagation();
        $('#bookingModal').modal('show');
    });

    $('.floating-tab-item[data-action="messenger"]').on('click', function () {
        window.open('https://m.me/your-page', '_blank');
    });

    $('.floating-tab-item[data-action="hotline"]').on('click', function () {
        window.open('tel:0123456789', '_self');
    });

    $('.floating-tab-item[data-action="zalo"]').on('click', function () {
        window.open('https://zalo.me/0123456789', '_blank');
    });

    $('.floating-tab-item[data-action="directions"]').on('click', function () {
        window.open('https://maps.google.com/?q=your-address', '_blank');
    });
}

// Expose helpers globally when needed elsewhere
window.showLoginModal = showLoginModal;


