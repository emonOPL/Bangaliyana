/**
 * Modern Theme Management System
 * Handles dark/light mode toggle and user preferences
 */

class ThemeManager {
    constructor() {
        this.STORAGE_KEY = 'bangaliyana-theme';
        this.currentTheme = this.getStoredTheme() || 'light';
        this.init();
    }

    init() {
        // Apply stored theme on page load
        this.applyTheme(this.currentTheme);
        
        // Initialize theme toggle buttons
        this.initializeToggleButtons();
        
        // Listen for system theme changes
        this.listenForSystemThemeChanges();
        
        // Auto-detect system theme if no preference is stored
        if (!this.getStoredTheme()) {
            this.detectSystemTheme();
        }
    }

    getStoredTheme() {
        return localStorage.getItem(this.STORAGE_KEY);
    }

    setStoredTheme(theme) {
        localStorage.setItem(this.STORAGE_KEY, theme);
    }

    detectSystemTheme() {
        if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
            this.setTheme('dark');
        } else {
            this.setTheme('light');
        }
    }

    listenForSystemThemeChanges() {
        if (window.matchMedia) {
            const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
            mediaQuery.addEventListener('change', (e) => {
                if (!this.getStoredTheme()) {
                    this.setTheme(e.matches ? 'dark' : 'light');
                }
            });
        }
    }

    applyTheme(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        this.currentTheme = theme;
        this.updateToggleButtons();
        
        // Dispatch custom event for theme change
        window.dispatchEvent(new CustomEvent('themeChanged', {
            detail: { theme: theme }
        }));
    }

    setTheme(theme) {
        this.setStoredTheme(theme);
        this.applyTheme(theme);
        
        // Add smooth transition for theme changes
        document.body.style.transition = 'background-color 0.3s ease, color 0.3s ease';
        setTimeout(() => {
            document.body.style.transition = '';
        }, 300);
    }

    toggleTheme() {
        const newTheme = this.currentTheme === 'light' ? 'dark' : 'light';
        this.setTheme(newTheme);
    }

    initializeToggleButtons() {
        const toggleButtons = document.querySelectorAll('.theme-toggle');
        toggleButtons.forEach(button => {
            button.addEventListener('click', (e) => {
                e.preventDefault();
                this.toggleTheme();
            });
        });
    }

    updateToggleButtons() {
        const toggleButtons = document.querySelectorAll('.theme-toggle');
        toggleButtons.forEach(button => {
            const icon = button.querySelector('i');
            if (icon) {
                if (this.currentTheme === 'dark') {
                    icon.className = 'fas fa-sun';
                    button.title = 'Switch to light mode';
                } else {
                    icon.className = 'fas fa-moon';
                    button.title = 'Switch to dark mode';
                }
            }
        });
    }

    getCurrentTheme() {
        return this.currentTheme;
    }
}

/**
 * Modern UI Enhancements
 * Handles animations, interactions, and user experience improvements
 */

class UIManager {
    constructor() {
        this.init();
    }

    init() {
        this.initializeAnimations();
        this.initializeTooltips();
        this.initializeAlerts();
        this.initializeFormEnhancements();
        this.initializeLoadingStates();
        this.initializeScrollEffects();
    }

    initializeAnimations() {
        // Add fade-in animation to cards on scroll
        //this.observeElements('.card, .product-card', 'fade-in');
        
        // Add slide-in animation to form elements
        this.observeElements('form .form-group, form .mb-3', 'slide-in-up');
        
        // Add bounce-in animation to alerts
        this.observeElements('.alert', 'bounce-in');
    }

    observeElements(selector, animationClass) {
        const elements = document.querySelectorAll(selector);
        if (!elements.length) return;

        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.classList.add(animationClass);
                    observer.unobserve(entry.target);
                }
            });
        }, {
            threshold: 0.1,
            rootMargin: '50px'
        });

        elements.forEach(element => {
            observer.observe(element);
        });
    }

    initializeTooltips() {
        // Initialize Bootstrap tooltips
        const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
    }

    initializeAlerts() {
        // Auto-dismiss alerts after 5 seconds
        const alerts = document.querySelectorAll('.alert:not(.alert-permanent)');
        alerts.forEach(alert => {
            setTimeout(() => {
                this.fadeOutAlert(alert);
            }, 5000);
        });

        // Close button functionality
        document.addEventListener('click', (e) => {
            if (e.target.matches('.alert .btn-close')) {
                e.preventDefault();
                this.fadeOutAlert(e.target.closest('.alert'));
            }
        });
    }

    fadeOutAlert(alert) {
        if (!alert) return;
        
        alert.style.transition = 'opacity 0.3s ease, transform 0.3s ease';
        alert.style.opacity = '0';
        alert.style.transform = 'translateY(-10px)';
        
        setTimeout(() => {
            if (alert.parentNode) {
                alert.parentNode.removeChild(alert);
            }
        }, 300);
    }

    initializeFormEnhancements() {
        // Add floating label effect
        const formControls = document.querySelectorAll('.form-control, .form-select');
        formControls.forEach(control => {
            this.addFloatingLabel(control);
        });

        // Form validation feedback
        const forms = document.querySelectorAll('form');
        forms.forEach(form => {
            form.addEventListener('submit', (e) => {
                this.handleFormValidation(form);
            });
        });
    }

    addFloatingLabel(control) {
        const parent = control.parentElement;
        const label = parent.querySelector('label');
        
        if (!label || parent.classList.contains('floating-label-processed')) return;
        
        parent.classList.add('floating-label-processed', 'position-relative');
        
        const updateLabel = () => {
            if (control.value || control === document.activeElement) {
                parent.classList.add('has-value');
            } else {
                parent.classList.remove('has-value');
            }
        };

        control.addEventListener('focus', updateLabel);
        control.addEventListener('blur', updateLabel);
        control.addEventListener('input', updateLabel);
        
        // Initial state
        updateLabel();
    }

    handleFormValidation(form) {
        const inputs = form.querySelectorAll('.form-control, .form-select');
        inputs.forEach(input => {
            if (input.checkValidity()) {
                input.classList.remove('is-invalid');
                input.classList.add('is-valid');
            } else {
                input.classList.remove('is-valid');
                input.classList.add('is-invalid');
            }
        });
    }

    initializeLoadingStates() {
        // Add loading states to buttons on form submission
        document.addEventListener('submit', (e) => {
            const form = e.target;
            const submitBtn = form.querySelector('button[type="submit"], input[type="submit"]');
            
            if (submitBtn && !submitBtn.disabled) {
                this.showLoadingState(submitBtn);
            }
        });

        // Add loading states to AJAX buttons
        document.addEventListener('click', (e) => {
            if (e.target.matches('.btn[data-loading="true"]')) {
                this.showLoadingState(e.target);
            }
        });
    }

    showLoadingState(button) {
        if (button.dataset.loading === 'active') return;
        
        button.dataset.loading = 'active';
        button.dataset.originalText = button.innerHTML;
        button.disabled = true;
        
        button.innerHTML = `
            <span class="spinner me-2"></span>
            ${button.dataset.loadingText || 'Loading...'}
        `;
        
        // Auto-restore after 5 seconds (fallback)
        setTimeout(() => {
            this.hideLoadingState(button);
        }, 5000);
    }

    hideLoadingState(button) {
        if (button.dataset.loading !== 'active') return;
        
        button.disabled = false;
        button.innerHTML = button.dataset.originalText;
        delete button.dataset.loading;
        delete button.dataset.originalText;
    }

    initializeScrollEffects() {
        // Smooth scroll for anchor links
        document.addEventListener('click', (e) => {
            if (e.target.matches('a[href^="#"]')) {
                e.preventDefault();
                const target = document.querySelector(e.target.getAttribute('href'));
                if (target) {
                    target.scrollIntoView({
                        behavior: 'smooth',
                        block: 'start'
                    });
                }
            }
        });

        // Navbar transparency on scroll
        const navbar = document.querySelector('.navbar');
        if (navbar) {
            let lastScrollTop = 0;
            
            window.addEventListener('scroll', () => {
                const scrollTop = window.pageYOffset || document.documentElement.scrollTop;
                
                if (scrollTop > 100) {
                    navbar.classList.add('navbar-scrolled');
                } else {
                    navbar.classList.remove('navbar-scrolled');
                }
                
                // Hide navbar on scroll down, show on scroll up
                if (scrollTop > lastScrollTop && scrollTop > 200) {
                    navbar.style.transform = 'translateY(-100%)';
                } else {
                    navbar.style.transform = 'translateY(0)';
                }
                
                lastScrollTop = scrollTop;
            });
        }
    }
}

/**
 * Shopping Cart Enhancements
 */

class CartManager {
    constructor() {
        this.init();
    }

    init() {
        this.initializeCartButtons();
        this.initializeQuantityControls();
    }

    initializeCartButtons() {
        document.addEventListener('click', (e) => {
            if (e.target.matches('.add-to-cart, .btn-add-cart')) {
                e.preventDefault();
                this.handleAddToCart(e.target);
            }
        });
    }

    handleAddToCart(button) {
        // Show loading state
        const originalText = button.innerHTML;
        button.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Adding...';
        button.disabled = true;

        // Simulate API call (replace with actual implementation)
        setTimeout(() => {
            // Success feedback
            button.innerHTML = '<i class="fas fa-check me-2"></i>Added!';
            button.classList.add('btn-success');
            
            // Reset after 2 seconds
            setTimeout(() => {
                button.innerHTML = originalText;
                button.disabled = false;
                button.classList.remove('btn-success');
            }, 2000);
            
            // Update cart count (if cart count element exists)
            this.updateCartCount();
        }, 1000);
    }

    updateCartCount() {
        // This would typically make an API call to get updated cart count
        const cartBadge = document.querySelector('.cart-count, .badge');
        if (cartBadge) {
            const currentCount = parseInt(cartBadge.textContent) || 0;
            cartBadge.textContent = currentCount + 1;
            cartBadge.classList.add('bounce-in');
            
            setTimeout(() => {
                cartBadge.classList.remove('bounce-in');
            }, 600);
        }
    }

    initializeQuantityControls() {
        document.addEventListener('click', (e) => {
            if (e.target.matches('.qty-btn-plus')) {
                e.preventDefault();
                this.updateQuantity(e.target, 1);
            } else if (e.target.matches('.qty-btn-minus')) {
                e.preventDefault();
                this.updateQuantity(e.target, -1);
            }
        });
    }

    updateQuantity(button, change) {
        const input = button.parentElement.querySelector('input[type="number"]');
        if (input) {
            const currentValue = parseInt(input.value) || 1;
            const newValue = Math.max(1, currentValue + change);
            input.value = newValue;
            
            // Trigger change event
            input.dispatchEvent(new Event('change'));
        }
    }
}

/**
 * Initialize everything when DOM is loaded
 */

document.addEventListener('DOMContentLoaded', function() {
    // Initialize all managers
    window.themeManager = new ThemeManager();
    window.uiManager = new UIManager();
    window.cartManager = new CartManager();
    
    // Global error handling
    window.addEventListener('error', function(e) {
        console.error('JavaScript Error:', e.error);
        // You could send errors to a logging service here
    });
    
    // Performance monitoring (optional)
    if ('performance' in window) {
        window.addEventListener('load', function() {
            setTimeout(function() {
                const perfData = performance.timing;
                const loadTime = perfData.loadEventEnd - perfData.navigationStart;
                console.log('Page Load Time:', loadTime + 'ms');
            }, 0);
        });
    }
    
    console.log('🎉 Bangaliyana UI System Initialized');
});

/**
 * Utility functions
 */

// Debounce function for performance
function debounce(func, wait, immediate) {
    let timeout;
    return function executedFunction() {
        const context = this;
        const args = arguments;
        const later = function() {
            timeout = null;
            if (!immediate) func.apply(context, args);
        };
        const callNow = immediate && !timeout;
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
        if (callNow) func.apply(context, args);
    };
}

// Show toast notification
function showToast(message, type = 'info', duration = 3000) {
    const toast = document.createElement('div');
    toast.className = `alert alert-${type} toast-notification`;
    toast.innerHTML = `
        <i class="fas fa-${getToastIcon(type)} me-2"></i>
        ${message}
    `;
    
    // Position toast
    toast.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        z-index: 9999;
        min-width: 300px;
        animation: slideInRight 0.3s ease;
    `;
    
    document.body.appendChild(toast);
    
    // Auto remove
    setTimeout(() => {
        toast.style.animation = 'slideOutRight 0.3s ease';
        setTimeout(() => {
            if (toast.parentNode) {
                toast.parentNode.removeChild(toast);
            }
        }, 300);
    }, duration);
}

function getToastIcon(type) {
    switch (type) {
        case 'success': return 'check-circle';
        case 'warning': return 'exclamation-triangle';
        case 'danger': return 'times-circle';
        case 'info': return 'info-circle';
        default: return 'info-circle';
    }
}

// Add CSS animations for toasts
const toastStyles = document.createElement('style');
toastStyles.textContent = `
    @keyframes slideInRight {
        from { transform: translateX(100%); opacity: 0; }
        to { transform: translateX(0); opacity: 1; }
    }
    
    @keyframes slideOutRight {
        from { transform: translateX(0); opacity: 1; }
        to { transform: translateX(100%); opacity: 0; }
    }
    
    .toast-notification {
        box-shadow: 0 4px 6px -1px rgb(0 0 0 / 0.1), 0 2px 4px -2px rgb(0 0 0 / 0.1);
    }
`;
document.head.appendChild(toastStyles);