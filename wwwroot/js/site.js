// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Shop Conflict Modal Handler
var pendingAddToCart = {
    productId: null,
    quantity: 1,
    button: null,
    originalHtml: null
};

function showShopConflictModal(existingShopName, productId, quantity, button, originalHtml, message) {
    pendingAddToCart.productId = productId;
    pendingAddToCart.quantity = quantity;
    pendingAddToCart.button = button;
    pendingAddToCart.originalHtml = originalHtml;

    // Update modal content with dynamic message
    if (existingShopName) {
        $('#shopConflictShopName').text(existingShopName);
    }

    // Show custom message if provided
    if (message) {
        $('#shopConflictMessage').html(message);
    }

    // Show modal
    var modal = new bootstrap.Modal(document.getElementById('shopConflictModal'));
    modal.show();
}

function confirmShopChange() {
    if (!pendingAddToCart.productId) return;

    var button = pendingAddToCart.button;
    if (button) {
        button.prop('disabled', true);
        button.html('<i class="fas fa-spinner fa-spin"></i>');
    }

    $.ajax({
        url: '/Customer/Home/ConfirmShopChange',
        type: 'POST',
        data: {
            productId: pendingAddToCart.productId,
            quantity: pendingAddToCart.quantity
        },
        success: function (response) {
            if (response.success) {
                alertify.success(response.message);

                // Update cart count in navbar
                var cartBadge = $('.cart-count');
                if (cartBadge.length > 0) {
                    cartBadge.text(response.cartCount);
                    cartBadge.removeClass('d-none');
                }
            } else {
                alertify.error(response.message);
            }
        },
        error: function () {
            alertify.error('An error occurred. Please try again.');
        },
        complete: function () {
            // Restore button state
            if (button && pendingAddToCart.originalHtml) {
                button.prop('disabled', false);
                button.html(pendingAddToCart.originalHtml);
            }

            // Close modal
            var modal = bootstrap.Modal.getInstance(document.getElementById('shopConflictModal'));
            if (modal) modal.hide();

            // Reset pending data
            pendingAddToCart = { productId: null, quantity: 1, button: null, originalHtml: null };
        }
    });
}

function cancelShopChange() {
    // Restore button state
    if (pendingAddToCart.button && pendingAddToCart.originalHtml) {
        pendingAddToCart.button.prop('disabled', false);
        pendingAddToCart.button.html(pendingAddToCart.originalHtml);
    }

    // Close modal
    var modal = bootstrap.Modal.getInstance(document.getElementById('shopConflictModal'));
    if (modal) modal.hide();

    // Reset pending data
    pendingAddToCart = { productId: null, quantity: 1, button: null, originalHtml: null };
}

// Add to Cart AJAX Handler
$(document).ready(function () {
    // Handle Add to Cart button clicks
    $(document).on('click', '.add-to-cart-btn', function (e) {
        e.preventDefault();
        var button = $(this);
        var productId = button.data('product-id');
        var quantity = button.data('quantity') || 1;
        var originalHtml = button.html();

        // Show loading state
        button.prop('disabled', true);
        button.html('<i class="fas fa-spinner fa-spin"></i>');

        $.ajax({
            url: '/Customer/Home/AddToCartAjax',
            type: 'POST',
            data: { productId: productId, quantity: quantity },
            success: function (response) {
                if (response.success) {
                    alertify.success(response.message);

                    // Update cart count in navbar if element exists
                    var cartBadge = $('.cart-count');
                    if (cartBadge.length > 0) {
                        cartBadge.text(response.cartCount);
                        cartBadge.removeClass('d-none');
                    }

                    // Restore button state
                    button.prop('disabled', false);
                    button.html(originalHtml);
                } else if (response.shopConflict) {
                    // Show shop conflict modal with dynamic message
                    showShopConflictModal(
                        response.existingShopName,
                        productId,
                        quantity,
                        button,
                        originalHtml,
                        response.message
                    );
                } else {
                    alertify.error(response.message);
                    // Restore button state
                    button.prop('disabled', false);
                    button.html(originalHtml);
                }
            },
            error: function () {
                alertify.error('An error occurred. Please try again.');
                // Restore button state
                button.prop('disabled', false);
                button.html(originalHtml);
            }
        });
    });

    // Handle modal cancel button click
    $(document).on('click', '#cancelShopChange', cancelShopChange);

    // Handle modal confirm button click
    $(document).on('click', '#confirmShopChange', confirmShopChange);

    // Handle modal backdrop click (when modal is dismissed)
    $('#shopConflictModal').on('hidden.bs.modal', function () {
        if (pendingAddToCart.button && pendingAddToCart.originalHtml) {
            pendingAddToCart.button.prop('disabled', false);
            pendingAddToCart.button.html(pendingAddToCart.originalHtml);
        }
        pendingAddToCart = { productId: null, quantity: 1, button: null, originalHtml: null };
    });

    // Handle Wishlist button clicks
    $(document).on('click', '.wishlist-btn', function (e) {
        e.preventDefault();
        var button = $(this);
        var productId = button.data('product-id');
        var icon = button.find('i');

        $.ajax({
            url: '/Customer/Wishlist/Toggle',
            type: 'POST',
            data: { productId: productId },
            success: function (response) {
                if (response.success) {
                    alertify.success(response.message);
                    if (response.isInWishlist) {
                        button.addClass('active');
                        icon.removeClass('far fa-heart').addClass('fas fa-heart text-danger');
                        button.attr('title', 'Remove from Wishlist');
                    } else {
                        button.removeClass('active');
                        icon.removeClass('fas fa-heart text-danger').addClass('far fa-heart');
                        button.attr('title', 'Add to Wishlist');
                    }
                } else if (response.requireLogin) {
                    alertify.warning(response.message);
                    window.location.href = '/Identity/Account/Login';
                } else {
                    alertify.error(response.message);
                }
            },
            error: function () {
                alertify.error('An error occurred. Please try again.');
            }
        });
    });

    // Handle Compare button clicks
    $(document).on('click', '.compare-btn', function (e) {
        e.preventDefault();
        var button = $(this);
        var productId = button.data('product-id');
        var icon = button.find('i');
        var isLargeButton = button.hasClass('btn-lg'); // Details page button

        $.ajax({
            url: '/Customer/Compare/Toggle',
            type: 'POST',
            data: { productId: productId },
            success: function (response) {
                if (response.success) {
                    alertify.success(response.message);
                    if (response.isInCompare) {
                        button.addClass('active');
                        if (isLargeButton) {
                            icon.addClass('text-white');
                        } else {
                            icon.addClass('text-primary');
                        }
                        button.attr('title', 'Remove from Compare');
                    } else {
                        button.removeClass('active');
                        icon.removeClass('text-primary text-white');
                        button.attr('title', 'Add to Compare');
                    }
                    // Update compare count badge and floating bar
                    updateCompareCount(response.compareCount);
                } else {
                    alertify.error(response.message);
                }
            },
            error: function () {
                alertify.error('An error occurred. Please try again.');
            }
        });
    });
});

// Update Compare Count Badge and Floating Bar
function updateCompareCount(count) {
    // Update navbar badge
    var compareBadge = $('.compare-count');
    if (compareBadge.length > 0) {
        if (count > 0) {
            compareBadge.text(count).removeClass('d-none');
        } else {
            compareBadge.addClass('d-none');
        }
    }

    // Update floating compare bar (on Details page)
    var floatingBar = $('#floatingCompareBar');
    var countBadge = $('#compareCountBadge');
    if (floatingBar.length > 0 && countBadge.length > 0) {
        countBadge.text(count);
        if (count > 0) {
            floatingBar.addClass('show');
        } else {
            floatingBar.removeClass('show');
        }
    }
}

// Clear Compare List
function clearCompare() {
    $.ajax({
        url: '/Customer/Compare/Clear',
        type: 'POST',
        success: function (response) {
            if (response.success) {
                // Reset all compare buttons on page
                $('.compare-btn').removeClass('active');
                $('.compare-btn i').removeClass('text-primary text-white');
                $('.compare-btn').attr('title', 'Add to Compare');

                // Update floating compare bar
                updateCompareCount(0);
                alertify.success(response.message);
            }
        },
        error: function () {
            alertify.error('An error occurred. Please try again.');
        }
    });
}
