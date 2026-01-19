/**
 * Phone Number, Postal Code, and Password Validation for Bangladesh
 * - Phone: Only digits, max 11 characters, must start with 013/014/015/016/017/018/019
 * - Postal Code: Only digits
 * - Password: Min 1 uppercase, 1 lowercase, 1 digit, 1 special character
 */

(function () {
    'use strict';

    // Bangladesh mobile number prefixes
    const BD_PHONE_PREFIXES = ['013', '014', '015', '016', '017', '018', '019'];

    // Localization messages (can be overridden by setting window.ValidationMessages)
    const defaultMessages = {
        phoneRequired: 'Phone number is required',
        phoneMustBe11Digits: 'Phone number must be exactly 11 digits',
        phoneInvalidPrefix: 'Phone must start with 013, 014, 015, 016, 017, 018, or 019',
        passwordRequired: 'Password is required',
        passwordMustContain: 'Password must contain: ',
        uppercase: '1 uppercase letter',
        lowercase: '1 lowercase letter',
        digit: '1 digit',
        specialChar: '1 special character'
    };

    // Get localized messages (use window.ValidationMessages if available)
    function getMessages() {
        return window.ValidationMessages || defaultMessages;
    }

    // Initialize validation on DOM ready
    document.addEventListener('DOMContentLoaded', function () {
        initPhoneValidation();
        initPostalCodeValidation();
        initPasswordValidation();
    });

    /**
     * Check if input is a phone number field (not password or other fields)
     */
    function isPhoneNumberInput(input) {
        const name = (input.name || '').toLowerCase();
        const id = (input.id || '').toLowerCase();
        const type = (input.type || '').toLowerCase();

        // Exclude password fields
        if (type === 'password' || name.includes('password') || id.includes('password')) {
            return false;
        }

        // Exclude email fields
        if (type === 'email' || name.includes('email') || id.includes('email')) {
            return false;
        }

        // Exclude OTP fields
        if (name.includes('otp') || id.includes('otp')) {
            return false;
        }

        // Check if it's a phone/mobile field
        if (type === 'tel') return true;
        if (name.includes('phone') || name.includes('mobile')) return true;
        if (id.includes('phone') || id.includes('mobile')) return true;
        if (input.classList.contains('phone-input') || input.classList.contains('bd-phone')) return true;

        return false;
    }

    /**
     * Initialize phone number validation for all phone input fields
     */
    function initPhoneValidation() {
        // Find all potential phone input fields
        const phoneSelectors = [
            'input[name*="Phone"]',
            'input[name*="phone"]',
            'input[name*="Mobile"]',
            'input[name*="mobile"]',
            'input[type="tel"]',
            'input[id*="phone"]',
            'input[id*="Phone"]',
            'input[id*="mobile"]',
            'input[id*="Mobile"]',
            '.phone-input',
            '.bd-phone'
        ];

        const potentialPhoneInputs = document.querySelectorAll(phoneSelectors.join(', '));

        potentialPhoneInputs.forEach(function (input) {
            // Skip if already initialized or not a phone input
            if (input.dataset.phoneValidated) return;
            if (!isPhoneNumberInput(input)) return;

            input.dataset.phoneValidated = 'true';

            // Set attributes
            input.setAttribute('maxlength', '11');
            input.setAttribute('inputmode', 'numeric');
            input.setAttribute('pattern', '^01[3-9]\\d{8}$');
            input.setAttribute('autocomplete', 'tel');

            // Only allow digits on keypress
            input.addEventListener('keypress', function (e) {
                if (!/^\d$/.test(e.key) && e.key !== 'Backspace' && e.key !== 'Delete' && e.key !== 'Tab' && e.key !== 'Enter') {
                    e.preventDefault();
                }
            });

            // Filter out non-digits on input (handles paste)
            input.addEventListener('input', function (e) {
                let value = this.value.replace(/\D/g, '');
                if (value.length > 11) {
                    value = value.substring(0, 11);
                }
                if (this.value !== value) {
                    this.value = value;
                }
                validatePhoneInput(this);
            });

            // Validate on blur
            input.addEventListener('blur', function () {
                validatePhoneInput(this);
            });

            // Add validation message container if not exists
            ensureValidationContainer(input, 'phone');
        });
    }

    /**
     * Validate a phone input field
     */
    function validatePhoneInput(input) {
        // Clean the value - remove ALL non-digit characters first
        let value = input.value.replace(/\D/g, '');

        // Update input value if it was different (had non-digits)
        if (input.value !== value) {
            input.value = value;
        }

        const container = input.closest('.form-group, .mb-3')?.parentElement || input.parentElement;
        let errorSpan = container.querySelector('.phone-validation-error');

        // Also check in the immediate parent and input-group parent
        if (!errorSpan) {
            const inputGroup = input.closest('.input-group');
            if (inputGroup && inputGroup.parentElement) {
                errorSpan = inputGroup.parentElement.querySelector('.phone-validation-error');
            }
        }
        if (!errorSpan) {
            errorSpan = input.parentElement.querySelector('.phone-validation-error');
        }

        // Clear previous error state immediately
        input.classList.remove('is-invalid', 'is-valid');
        if (errorSpan) {
            errorSpan.textContent = '';
            errorSpan.style.display = 'none';
        }

        if (!value) {
            // Empty - check if required
            if (input.hasAttribute('required') || input.dataset.required === 'true') {
                showError(input, errorSpan, getMessages().phoneRequired);
                return false;
            }
            return true;
        }

        // Check length - must be exactly 11 digits
        if (value.length !== 11) {
            showError(input, errorSpan, getMessages().phoneMustBe11Digits);
            return false;
        }

        // Check prefix
        const prefix = value.substring(0, 3);
        if (!BD_PHONE_PREFIXES.includes(prefix)) {
            showError(input, errorSpan, getMessages().phoneInvalidPrefix);
            return false;
        }

        // Valid
        input.classList.add('is-valid');
        return true;
    }

    /**
     * Initialize postal code validation for all postal code input fields
     */
    function initPostalCodeValidation() {
        // Find all postal code input fields
        const postalSelectors = [
            'input[name*="Postal"]',
            'input[name*="postal"]',
            'input[name*="PostalCode"]',
            'input[name*="postalCode"]',
            'input[name*="ZipCode"]',
            'input[name*="zipCode"]',
            'input[id*="postal"]',
            'input[id*="Postal"]',
            '.postal-input',
            '.postal-code'
        ];

        const postalInputs = document.querySelectorAll(postalSelectors.join(', '));

        postalInputs.forEach(function (input) {
            // Skip if already initialized
            if (input.dataset.postalValidated) return;
            input.dataset.postalValidated = 'true';

            // Set attributes
            input.setAttribute('inputmode', 'numeric');
            input.setAttribute('maxlength', '10');

            // Only allow digits on keypress
            input.addEventListener('keypress', function (e) {
                if (!/^\d$/.test(e.key) && e.key !== 'Backspace' && e.key !== 'Delete' && e.key !== 'Tab' && e.key !== 'Enter') {
                    e.preventDefault();
                }
            });

            // Filter out non-digits on input (handles paste)
            input.addEventListener('input', function (e) {
                const value = this.value.replace(/\D/g, '');
                if (this.value !== value) {
                    this.value = value;
                }
            });
        });
    }

    /**
     * Initialize password validation for all password input fields
     * Requirements: Min 1 uppercase, 1 lowercase, 1 digit, 1 special character
     */
    function initPasswordValidation() {
        const passwordInputs = document.querySelectorAll('input[type="password"], input[name*="Password"], input[name*="password"]');

        passwordInputs.forEach(function (input) {
            // Skip if already initialized
            if (input.dataset.passwordValidated) return;
            input.dataset.passwordValidated = 'true';

            // Validate on blur
            input.addEventListener('blur', function () {
                validatePasswordInput(this);
            });

            // Also validate on input for real-time feedback
            input.addEventListener('input', function () {
                // Only show validation after user has typed something
                if (this.value.length > 0) {
                    validatePasswordInput(this);
                }
            });

            // Add validation message container
            ensureValidationContainer(input, 'password');
        });
    }

    /**
     * Validate a password input field
     */
    function validatePasswordInput(input) {
        const value = input.value;
        const container = input.closest('.form-group, .mb-3, .input-group')?.parentElement || input.parentElement;
        let errorSpan = container.querySelector('.password-validation-error');

        // Remove previous error state
        input.classList.remove('is-invalid', 'is-valid');

        if (!value) {
            // Empty - check if required
            if (input.hasAttribute('required') || input.dataset.required === 'true') {
                showError(input, errorSpan, getMessages().passwordRequired);
                return false;
            }
            hideError(input, errorSpan);
            return true;
        }

        // Check password requirements
        const msgs = getMessages();
        const errors = [];

        if (!/[A-Z]/.test(value)) {
            errors.push(msgs.uppercase);
        }
        if (!/[a-z]/.test(value)) {
            errors.push(msgs.lowercase);
        }
        if (!/[0-9]/.test(value)) {
            errors.push(msgs.digit);
        }
        if (!/[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(value)) {
            errors.push(msgs.specialChar);
        }

        if (errors.length > 0) {
            showError(input, errorSpan, msgs.passwordMustContain + errors.join(', '));
            return false;
        }

        // Valid
        hideError(input, errorSpan);
        input.classList.add('is-valid');
        return true;
    }

    /**
     * Ensure validation error container exists
     */
    function ensureValidationContainer(input, type) {
        const container = input.closest('.form-group, .mb-3')?.parentElement || input.parentElement;
        if (!container.querySelector(`.${type}-validation-error`)) {
            const errorSpan = document.createElement('span');
            errorSpan.className = `${type}-validation-error text-danger small d-block mt-1`;
            errorSpan.style.display = 'none';

            // Insert after input group or input
            const inputGroup = input.closest('.input-group');
            if (inputGroup) {
                inputGroup.parentElement.appendChild(errorSpan);
            } else {
                input.parentElement.appendChild(errorSpan);
            }
        }
    }

    /**
     * Show validation error
     */
    function showError(input, errorSpan, message) {
        input.classList.add('is-invalid');
        input.classList.remove('is-valid');
        if (errorSpan) {
            errorSpan.textContent = message;
            errorSpan.style.display = 'block';
        }
    }

    /**
     * Hide validation error
     */
    function hideError(input, errorSpan) {
        input.classList.remove('is-invalid');
        if (errorSpan) {
            errorSpan.textContent = '';
            errorSpan.style.display = 'none';
        }
    }

    /**
     * Public API for manual validation
     */
    window.BDPhoneValidation = {
        validatePhone: function (input) {
            return validatePhoneInput(input);
        },
        validatePassword: function (input) {
            return validatePasswordInput(input);
        },
        validateAllPhones: function () {
            let allValid = true;
            document.querySelectorAll('input[data-phone-validated="true"]').forEach(function (input) {
                if (!validatePhoneInput(input)) {
                    allValid = false;
                }
            });
            return allValid;
        },
        validateAllPasswords: function () {
            let allValid = true;
            document.querySelectorAll('input[data-password-validated="true"]').forEach(function (input) {
                if (!validatePasswordInput(input)) {
                    allValid = false;
                }
            });
            return allValid;
        },
        isValidBDPhone: function (number) {
            if (!number || typeof number !== 'string') return false;
            const cleaned = number.replace(/\D/g, '');
            if (cleaned.length !== 11) return false;
            const prefix = cleaned.substring(0, 3);
            return BD_PHONE_PREFIXES.includes(prefix);
        },
        isValidPassword: function (password) {
            if (!password || typeof password !== 'string') return false;
            return /[A-Z]/.test(password) &&
                   /[a-z]/.test(password) &&
                   /[0-9]/.test(password) &&
                   /[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(password);
        },
        reinitialize: function () {
            initPhoneValidation();
            initPostalCodeValidation();
            initPasswordValidation();
        }
    };

    // Intercept form submissions to validate phone numbers and passwords
    document.addEventListener('submit', function (e) {
        const form = e.target;
        const phoneInputs = form.querySelectorAll('input[data-phone-validated="true"]');
        const passwordInputs = form.querySelectorAll('input[data-password-validated="true"]');
        let hasInvalid = false;

        phoneInputs.forEach(function (input) {
            if (!validatePhoneInput(input)) {
                hasInvalid = true;
            }
        });

        passwordInputs.forEach(function (input) {
            if (!validatePasswordInput(input)) {
                hasInvalid = true;
            }
        });

        if (hasInvalid) {
            e.preventDefault();
            e.stopPropagation();

            // Focus first invalid input
            const firstInvalid = form.querySelector('input.is-invalid');
            if (firstInvalid) {
                firstInvalid.focus();
            }
        }
    }, true);

})();
