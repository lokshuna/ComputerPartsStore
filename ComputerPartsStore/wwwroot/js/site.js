// Site-wide JavaScript functions

$(document).ready(function () {
    // Initialize tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Initialize popovers
    var popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
    var popoverList = popoverTriggerList.map(function (popoverTriggerEl) {
        return new bootstrap.Popover(popoverTriggerEl);
    });

    // Auto-hide alerts after 5 seconds
    $('.alert:not(.alert-permanent)').delay(5000).fadeOut('slow');

    // Update cart count on page load
    updateCartCount();

    // Add loading state to form submissions
    $('form').on('submit', function () {
        $(this).find('button[type="submit"]').addClass('loading').prop('disabled', true);
    });

    // Add smooth scrolling to anchor links
    $('a[href*="#"]').not('[href="#"]').not('[href="#0"]').click(function (event) {
        if (location.pathname.replace(/^\//, '') == this.pathname.replace(/^\//, '') &&
            location.hostname == this.hostname) {
            var target = $(this.hash);
            target = target.length ? target : $('[name=' + this.hash.slice(1) + ']');
            if (target.length) {
                event.preventDefault();
                $('html, body').animate({
                    scrollTop: target.offset().top - 70
                }, 1000);
            }
        }
    });

    // Add ripple effect to buttons
    $('.btn').on('click', function (e) {
        var ripple = $('<span class="ripple"></span>');
        var btn = $(this);
        var btnOffset = btn.offset();
        var xPos = e.pageX - btnOffset.left;
        var yPos = e.pageY - btnOffset.top;

        ripple.css({
            position: 'absolute',
            top: yPos + 'px',
            left: xPos + 'px',
            width: '0',
            height: '0',
            borderRadius: '50%',
            background: 'rgba(255,255,255,0.5)',
            transform: 'scale(0)',
            animation: 'ripple 0.6s linear',
            pointerEvents: 'none'
        });

        btn.css('position', 'relative').append(ripple);

        setTimeout(function () {
            ripple.remove();
        }, 600);
    });
});

// Shopping Cart Functions
function addToCart(productId, quantity = 1) {
    $.ajax({
        url: '/Cart/AddToCart',
        type: 'POST',
        data: {
            productId: productId,
            quantity: quantity
        },
        success: function (response) {
            if (response.success) {
                updateCartCount(response.cartCount);
                showToast(response.message, 'success');

                // Animate the cart icon
                animateCartIcon();
            } else {
                showToast(response.message, 'error');
            }
        },
        error: function () {
            showToast('Помилка при додаванні товару до кошика', 'error');
        }
    });
}

function updateCartQuantity(productId, quantity) {
    quantity = parseInt(quantity);

    if (quantity < 1) {
        removeFromCart(productId);
        return;
    }

    if (quantity > 10) {
        showToast('Максимальна кількість товару - 10 штук', 'warning');
        return;
    }

    showLoading();

    $.ajax({
        url: '/Cart/UpdateQuantity',
        type: 'POST',
        data: {
            productId: productId,
            quantity: quantity
        },
        success: function (response) {
            hideLoading();

            if (response.success) {
                updateCartDisplay(response);
                showToast('Кількість товару оновлено', 'success');
            } else {
                showToast('Помилка при оновленні кількості', 'error');
                location.reload();
            }
        },
        error: function () {
            hideLoading();
            showToast('Помилка при оновленні кошика', 'error');
        }
    });
}

function removeFromCart(productId) {
    if (!confirm('Видалити товар з кошика?')) {
        return;
    }

    showLoading();

    $.ajax({
        url: '/Cart/RemoveFromCart',
        type: 'POST',
        data: { productId: productId },
        success: function (response) {
            hideLoading();

            if (response.success) {
                $('#cart-item-' + productId).fadeOut(300, function () {
                    $(this).remove();
                    updateCartDisplay(response);
                });
                showToast('Товар видалено з кошика', 'success');

                // Check if cart is empty
                if (response.cartCount === 0) {
                    setTimeout(() => location.reload(), 1000);
                }
            } else {
                showToast('Помилка при видаленні товару', 'error');
            }
        },
        error: function () {
            hideLoading();
            showToast('Помилка при видаленні товару', 'error');
        }
    });
}

function updateCartCount(count = null) {
    if (count !== null) {
        $('#cart-count').text(count);
        animateCartIcon();
    } else {
        // Load cart count from server
        $.get('/Cart/GetCartCount', function (count) {
            $('#cart-count').text(count);
        });
    }
}

function updateCartDisplay(response) {
    if (response.cartTotal !== undefined) {
        $('#total-amount').text(formatCurrency(response.cartTotal));
        $('#final-total').text(formatCurrency(response.cartTotal));
    }

    if (response.cartCount !== undefined) {
        $('#items-count').text(response.cartCount);
        updateCartCount(response.cartCount);
    }
}

function animateCartIcon() {
    $('#cart-count').addClass('animate__animated animate__bounce');
    setTimeout(() => {
        $('#cart-count').removeClass('animate__animated animate__bounce');
    }, 1000);
}

// Toast Notifications
function showToast(message, type = 'info', duration = 5000) {
    const toastId = 'toast-' + Date.now();
    const bgClass = {
        'success': 'bg-success',
        'error': 'bg-danger',
        'warning': 'bg-warning',
        'info': 'bg-info'
    }[type] || 'bg-info';

    const toast = $(`
        <div id="${toastId}" class="toast align-items-center text-white ${bgClass} border-0" 
             role="alert" aria-live="assertive" aria-atomic="true" 
             style="position: fixed; top: 20px; right: 20px; z-index: 1060; min-width: 300px;">
            <div class="d-flex">
                <div class="toast-body">
                    <i class="fas fa-${getToastIcon(type)} me-2"></i>
                    ${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" 
                        data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
        </div>
    `);

    $('body').append(toast);

    const bsToast = new bootstrap.Toast(toast[0], {
        delay: duration
    });

    bsToast.show();

    // Remove from DOM after hiding
    toast.on('hidden.bs.toast', function () {
        $(this).remove();
    });
}

function getToastIcon(type) {
    const icons = {
        'success': 'check-circle',
        'error': 'exclamation-circle',
        'warning': 'exclamation-triangle',
        'info': 'info-circle'
    };
    return icons[type] || 'info-circle';
}

// Loading States
function showLoading(target = 'body') {
    const loader = $(`
        <div class="loading-overlay">
            <div class="loading-spinner">
                <div class="spinner-border text-primary" role="status">
                    <span class="visually-hidden">Завантаження...</span>
                </div>
            </div>
        </div>
    `);

    $(target).append(loader);
}

function hideLoading(target = 'body') {
    $(target).find('.loading-overlay').fadeOut(300, function () {
        $(this).remove();
    });
}

// Utility Functions
function formatCurrency(amount) {
    return new Intl.NumberFormat('uk-UA', {
        style: 'decimal',
        minimumFractionDigits: 0,
        maximumFractionDigits: 0
    }).format(amount) + ' ₴';
}

function formatDate(date) {
    return new Intl.DateTimeFormat('uk-UA', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit'
    }).format(new Date(date));
}

function debounce(func, wait, immediate) {
    let timeout;
    return function executedFunction() {
        const context = this;
        const args = arguments;
        const later = function () {
            timeout = null;
            if (!immediate) func.apply(context, args);
        };
        const callNow = immediate && !timeout;
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
        if (callNow) func.apply(context, args);
    };
}

// Form Validation
function validateEmail(email) {
    const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return re.test(email);
}

function validatePhone(phone) {
    const re = /^380\d{9}$/;
    return re.test(phone.replace(/\D/g, ''));
}

// Keyboard Shortcuts
$(document).keydown(function (e) {
    // Ctrl+/ or Cmd+/ for search
    if ((e.ctrlKey || e.metaKey) && e.keyCode === 191) {
        e.preventDefault();
        $('input[name="search"]').focus();
    }

    // Escape to close modals
    if (e.keyCode === 27) {
        $('.modal.show').modal('hide');
    }
});

// Add ripple animation CSS
const rippleCSS = `
    @keyframes ripple {
        to {
            transform: scale(4);
            opacity: 0;
        }
    }
    
    .loading-overlay {
        position: fixed;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        background: rgba(0,0,0,0.5);
        display: flex;
        justify-content: center;
        align-items: center;
        z-index: 9999;
    }
    
    .loading-spinner {
        text-align: center;
    }
`;

// Inject CSS
if (!document.getElementById('site-css')) {
    const style = document.createElement('style');
    style.id = 'site-css';
    style.textContent = rippleCSS;
    document.head.appendChild(style);
}

// Export functions for use in other scripts
window.SiteUtils = {
    addToCart,
    updateCartQuantity,
    removeFromCart,
    showToast,
    showLoading,
    hideLoading,
    formatCurrency,
    formatDate,
    debounce,
    validateEmail,
    validatePhone
};