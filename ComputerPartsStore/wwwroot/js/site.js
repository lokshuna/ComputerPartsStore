// Simple Site JavaScript

// Global functions for cart management
function addToCart(productId, quantity) {
    quantity = quantity || 1;

    var xhr = new XMLHttpRequest();
    xhr.open('POST', '/Cart/AddToCart', true);
    xhr.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded');

    xhr.onreadystatechange = function () {
        if (xhr.readyState === 4) {
            if (xhr.status === 200) {
                try {
                    var response = JSON.parse(xhr.responseText);
                    if (response.success) {
                        showToast(response.message, 'success');
                        updateCartCount(response.cartCount);
                    } else {
                        showToast(response.message || 'Помилка при додаванні товару', 'error');
                    }
                } catch (e) {
                    showToast('Помилка при додаванні товару', 'error');
                }
            } else {
                showToast('Помилка сервера', 'error');
            }
        }
    };

    var params = 'productId=' + productId + '&quantity=' + quantity;
    xhr.send(params);
}

function updateQuantity(productId, quantity) {
    if (quantity < 1) {
        removeFromCart(productId);
        return;
    }

    if (quantity > 10) {
        showToast('Максимальна кількість товару - 10 штук', 'warning');
        return;
    }

    var xhr = new XMLHttpRequest();
    xhr.open('POST', '/Cart/UpdateQuantity', true);
    xhr.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded');

    xhr.onreadystatechange = function () {
        if (xhr.readyState === 4 && xhr.status === 200) {
            try {
                var response = JSON.parse(xhr.responseText);
                if (response.success) {
                    updateCartDisplay(response);
                    showToast('Кількість оновлено', 'success');
                } else {
                    showToast('Помилка при оновленні', 'error');
                    location.reload();
                }
            } catch (e) {
                location.reload();
            }
        }
    };

    var params = 'productId=' + productId + '&quantity=' + quantity;
    xhr.send(params);
}

function removeFromCart(productId) {
    if (!confirm('Видалити товар з кошика?')) {
        return;
    }

    var xhr = new XMLHttpRequest();
    xhr.open('POST', '/Cart/RemoveFromCart', true);
    xhr.setRequestHeader('Content-Type', 'application/x-www-form-urlencoded');

    xhr.onreadystatechange = function () {
        if (xhr.readyState === 4 && xhr.status === 200) {
            try {
                var response = JSON.parse(xhr.responseText);
                if (response.success) {
                    var itemElement = document.getElementById('cart-item-' + productId);
                    if (itemElement) {
                        itemElement.style.display = 'none';
                    }
                    updateCartDisplay(response);
                    showToast('Товар видалено', 'success');

                    if (response.cartCount === 0) {
                        setTimeout(function () {
                            location.reload();
                        }, 1000);
                    }
                } else {
                    showToast('Помилка при видаленні', 'error');
                }
            } catch (e) {
                location.reload();
            }
        }
    };

    var params = 'productId=' + productId;
    xhr.send(params);
}

function updateCartCount(count) {
    var cartCountElement = document.getElementById('cart-count');
    if (cartCountElement) {
        if (count !== undefined) {
            cartCountElement.textContent = count;
            if (count > 0) {
                cartCountElement.classList.remove('d-none');
            } else {
                cartCountElement.classList.add('d-none');
            }
        } else {
            // Load from server
            var xhr = new XMLHttpRequest();
            xhr.open('GET', '/Cart/GetCartCount', true);
            xhr.onreadystatechange = function () {
                if (xhr.readyState === 4 && xhr.status === 200) {
                    var cartCount = parseInt(xhr.responseText) || 0;
                    cartCountElement.textContent = cartCount;
                    if (cartCount > 0) {
                        cartCountElement.classList.remove('d-none');
                    } else {
                        cartCountElement.classList.add('d-none');
                    }
                }
            };
            xhr.send();
        }
    }
}

function updateCartDisplay(response) {
    if (response.cartTotal !== undefined) {
        var totalElements = document.querySelectorAll('#total-amount, #final-total');
        for (var i = 0; i < totalElements.length; i++) {
            if (totalElements[i]) {
                totalElements[i].textContent = formatCurrency(response.cartTotal);
            }
        }
    }

    if (response.cartCount !== undefined) {
        var countElement = document.getElementById('items-count');
        if (countElement) {
            countElement.textContent = response.cartCount;
        }
        updateCartCount(response.cartCount);
    }
}

function formatCurrency(amount) {
    return amount.toLocaleString('uk-UA') + ' ₴';
}

function showToast(message, type) {
    type = type || 'info';

    var toastContainer = document.getElementById('toast-container');
    if (!toastContainer) {
        toastContainer = document.createElement('div');
        toastContainer.id = 'toast-container';
        toastContainer.className = 'position-fixed top-0 end-0 p-3';
        toastContainer.style.zIndex = '1055';
        document.body.appendChild(toastContainer);
    }

    var icons = {
        'success': 'fa-check-circle',
        'error': 'fa-exclamation-circle',
        'warning': 'fa-exclamation-triangle',
        'info': 'fa-info-circle'
    };

    var bgClasses = {
        'success': 'bg-success',
        'error': 'bg-danger',
        'warning': 'bg-warning',
        'info': 'bg-info'
    };

    var toastElement = document.createElement('div');
    toastElement.className = 'toast align-items-center text-white ' + bgClasses[type] + ' border-0';
    toastElement.setAttribute('role', 'alert');

    toastElement.innerHTML =
        '<div class="d-flex">' +
        '<div class="toast-body">' +
        '<i class="fas ' + icons[type] + ' me-2"></i>' +
        message +
        '</div>' +
        '<button type="button" class="btn-close btn-close-white me-2 m-auto" onclick="this.parentElement.parentElement.remove()"></button>' +
        '</div>';

    toastContainer.appendChild(toastElement);

    // Auto remove after 5 seconds
    setTimeout(function () {
        if (toastElement.parentNode) {
            toastElement.remove();
        }
    }, 5000);
}

// Quick login function for demo
function quickLogin(username, password) {
    var form = document.createElement('form');
    form.method = 'POST';
    form.action = '/Account/Login';
    form.style.display = 'none';

    var loginInput = document.createElement('input');
    loginInput.type = 'hidden';
    loginInput.name = 'Login';
    loginInput.value = username;

    var passwordInput = document.createElement('input');
    passwordInput.type = 'hidden';
    passwordInput.name = 'Password';
    passwordInput.value = password;

    // Get anti-forgery token
    var tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    if (tokenInput) {
        var newTokenInput = document.createElement('input');
        newTokenInput.type = 'hidden';
        newTokenInput.name = '__RequestVerificationToken';
        newTokenInput.value = tokenInput.value;
        form.appendChild(newTokenInput);
    }

    form.appendChild(loginInput);
    form.appendChild(passwordInput);

    document.body.appendChild(form);
    form.submit();
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function () {
    // Initialize tooltips if Bootstrap is available
    if (typeof bootstrap !== 'undefined') {
        var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });

        var popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
        var popoverList = popoverTriggerList.map(function (popoverTriggerEl) {
            return new bootstrap.Popover(popoverTriggerEl);
        });
    }

    // Update cart count on page load
    updateCartCount();

    // Auto-hide alerts after 5 seconds
    var alerts = document.querySelectorAll('.alert:not(.alert-permanent)');
    setTimeout(function () {
        for (var i = 0; i < alerts.length; i++) {
            alerts[i].style.opacity = '0';
            setTimeout(function (alert) {
                return function () {
                    if (alert.parentNode) {
                        alert.remove();
                    }
                };
            }(alerts[i]), 500);
        }
    }, 5000);

    // Add form submission loading states
    var forms = document.querySelectorAll('form');
    for (var i = 0; i < forms.length; i++) {
        forms[i].addEventListener('submit', function () {
            var submitBtn = this.querySelector('button[type="submit"], input[type="submit"]');
            if (submitBtn && !submitBtn.disabled) {
                submitBtn.disabled = true;
                var originalText = submitBtn.innerHTML;
                submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Обробка...';

                // Re-enable after delay
                setTimeout(function () {
                    submitBtn.disabled = false;
                    submitBtn.innerHTML = originalText;
                }, 10000);
            }
        });
    }

    // Add smooth scroll to anchor links
    var anchorLinks = document.querySelectorAll('a[href*="#"]');
    for (var i = 0; i < anchorLinks.length; i++) {
        anchorLinks[i].addEventListener('click', function (e) {
            var href = this.getAttribute('href');
            if (href !== '#' && href !== '#0') {
                var target = document.querySelector(href);
                if (target) {
                    e.preventDefault();
                    target.scrollIntoView({
                        behavior: 'smooth',
                        block: 'start'
                    });
                }
            }
        });
    }
});

// Keyboard shortcuts
document.addEventListener('keydown', function (e) {
    // Escape to close modals
    if (e.key === 'Escape' || e.keyCode === 27) {
        var modals = document.querySelectorAll('.modal.show');
        for (var i = 0; i < modals.length; i++) {
            if (typeof bootstrap !== 'undefined') {
                var modal = bootstrap.Modal.getInstance(modals[i]);
                if (modal) {
                    modal.hide();
                }
            }
        }
    }

    // Ctrl+/ for search
    if ((e.ctrlKey || e.metaKey) && (e.key === '/' || e.keyCode === 191)) {
        e.preventDefault();
        var searchInput = document.querySelector('input[name="search"]');
        if (searchInput) {
            searchInput.focus();
        }
    }
});