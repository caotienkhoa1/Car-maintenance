/**
 * Sidebar Menu Management
 * Xử lý mở/đóng menu và lưu trạng thái
 */

(function() {
    'use strict';

    // Khởi tạo khi DOM ready
    $(document).ready(function() {
        initializeSidebarMenu();
        setupAjaxNavigation();
    });

    /**
     * Khởi tạo sidebar menu
     */
    function initializeSidebarMenu() {
        // Khôi phục trạng thái ngay lập tức (trước khi render)
        restoreMenuState();
        
        // Sau đó setup các event handlers
        setTimeout(function() {
            // Xử lý click vào menu header để toggle
            setupMenuToggle();
            
            // Lắng nghe sự kiện collapse để cập nhật icon và lưu trạng thái
            setupCollapseEvents();
            
            console.log('Sidebar menu initialized');
        }, 100);
    }

    /**
     * Setup toggle cho menu headers
     */
    function setupMenuToggle() {
        // Xóa data-bs-toggle để tự xử lý hoàn toàn
        $('.nav-section-header').removeAttr('data-bs-toggle');
        
        $('.nav-section-header').off('click.sidebar').on('click.sidebar', function(e) {
            e.preventDefault();
            e.stopPropagation();
            e.stopImmediatePropagation();
            
            const $header = $(this);
            const targetId = $header.attr('data-bs-target');
            if (!targetId) {
                console.warn('No target found for menu header');
                return false;
            }
            
            const $collapse = $(targetId);
            if ($collapse.length === 0) {
                console.warn('Target element not found:', targetId);
                return false;
            }
            
            // Kiểm tra trạng thái hiện tại
            const isExpanded = $collapse.hasClass('show');
            
            console.log('Toggling menu:', targetId, 'Current state:', isExpanded ? 'open' : 'closed');
            
            if (isExpanded) {
                // Đóng menu - tự xử lý với slideUp
                $collapse.slideUp(300, function() {
                    $(this).removeClass('show');
                    const menuId = $(this).attr('id');
                    saveMenuState(menuId, false);
                    updateChevronIcon($(this), false);
                    $header.attr('aria-expanded', 'false');
                });
            } else {
                // Mở menu - tự xử lý với slideDown
                $collapse.addClass('show').slideDown(300, function() {
                    const menuId = $(this).attr('id');
                    saveMenuState(menuId, true);
                    updateChevronIcon($(this), true);
                    $header.attr('aria-expanded', 'true');
                });
            }
            
            return false;
        });
    }

    /**
     * Setup events cho collapse
     */
    function setupCollapseEvents() {
        $('.nav-section .collapse').off('shown.bs.collapse hidden.bs.collapse')
            .on('shown.bs.collapse', function() {
                const menuId = $(this).attr('id');
                saveMenuState(menuId, true);
                updateChevronIcon($(this), true);
            })
            .on('hidden.bs.collapse', function() {
                const menuId = $(this).attr('id');
                saveMenuState(menuId, false);
                updateChevronIcon($(this), false);
            });
    }

    /**
     * Lưu trạng thái menu vào localStorage
     */
    function saveMenuState(menuId, isOpen) {
        try {
            let menuStates = JSON.parse(localStorage.getItem('sidebarMenuStates') || '{}');
            menuStates[menuId] = isOpen;
            localStorage.setItem('sidebarMenuStates', JSON.stringify(menuStates));
        } catch (e) {
            console.error('Error saving menu state:', e);
        }
    }

    /**
     * Khôi phục trạng thái menu từ localStorage
     * Chạy ngay lập tức để tránh "nhấp nháy"
     */
    function restoreMenuState() {
        try {
            const menuStates = JSON.parse(localStorage.getItem('sidebarMenuStates') || '{}');
            
            // Khôi phục ngay lập tức, không chờ animation
            $('.nav-section').each(function() {
                const $section = $(this);
                const $collapse = $section.find('.collapse');
                const menuId = $collapse.attr('id');
                
                if (menuId && menuStates.hasOwnProperty(menuId)) {
                    const shouldBeOpen = menuStates[menuId];
                    
                    // Set trạng thái ngay lập tức (không animation)
                    if (shouldBeOpen) {
                        $collapse.addClass('show').css('display', 'block');
                        $section.find('.nav-section-header').attr('aria-expanded', 'true');
                    } else {
                        $collapse.removeClass('show').css('display', 'none');
                        $section.find('.nav-section-header').attr('aria-expanded', 'false');
                    }
                    
                    // Cập nhật icon ngay lập tức
                    updateChevronIcon($collapse, shouldBeOpen);
                } else {
                    // Nếu chưa có trong localStorage, giữ nguyên trạng thái hiện tại
                    const isExpanded = $collapse.hasClass('show');
                    updateChevronIcon($collapse, isExpanded);
                }
            });
        } catch (e) {
            console.error('Error restoring menu state:', e);
        }
    }

    /**
     * Cập nhật icon mũi tên dựa trên trạng thái collapse
     */
    function updateChevronIcon($collapseElement, isExpanded) {
        const $header = $collapseElement.closest('.nav-section').find('.nav-section-header');
        const $chevron = $header.find('i:last-child');
        
        if (isExpanded) {
            $chevron.removeClass('fa-chevron-down').addClass('fa-chevron-up');
        } else {
            $chevron.removeClass('fa-chevron-up').addClass('fa-chevron-down');
        }
    }

    /**
     * Setup AJAX navigation để sidebar không bị reload
     * Tạm thời tắt để tránh lỗi, chỉ giữ khôi phục trạng thái nhanh
     */
    function setupAjaxNavigation() {
        // Tạm thời không dùng AJAX navigation vì có thể gây lỗi với các trang có form/script
        // Chỉ đảm bảo sidebar khôi phục trạng thái nhanh để không bị "nhấp nháy"
        console.log('Sidebar navigation: Using standard navigation with fast state restoration');
    }

})();

