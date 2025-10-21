// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Customer Management JavaScript
$(document).ready(function() {
    // Initialize customer management functionality
    initializeCustomerManagement();
});

function initializeCustomerManagement() {
    // Search functionality with debouncing
    let searchTimeout;
    $('#search').on('input', function() {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(function() {
            performSearch();
        }, 500);
    });

    // Filter change handlers
    //$('#status, #address, #quarter, #year').on('change', function() {
    //    performSearch();
    //});

    // Select all functionality
    $('#selectAll').on('change', function() {
        $('.customer-checkbox').prop('checked', this.checked);
        updateBulkActions();
    });

    // Individual checkbox change
    $('.customer-checkbox').on('change', function() {
        updateBulkActions();
        updateSelectAllState();
    });

    // Bulk action handlers
    $('.dropdown-item[data-action]').on('click', function(e) {
        e.preventDefault();
        const action = $(this).data('action');
        const selectedIds = $('.customer-checkbox:checked').map(function() {
            return $(this).val();
        }).get();

        if (selectedIds.length === 0) {
            showAlert('Please select at least one customer.', 'warning');
            return;
        }

        if (confirm(`Are you sure you want to ${action} ${selectedIds.length} customer(s)?`)) {
            performBulkAction(action, selectedIds);
        }
    });

    // Form auto-save functionality
    $('.customer-form input, .customer-form textarea, .customer-form select').on('change', function() {
        autoSaveFormData();
    });

    // Load saved form data on page load
    loadFormData();
}

function performSearch() {
    const searchTerm = $('#search').val();
    const status = $('#status').val();

    // Build URL with current parameters
    const url = new URL(window.location);
    url.searchParams.set('search', searchTerm || '');
    url.searchParams.set('status', status || '');
    url.searchParams.set('page', '1'); // Reset to first page

    // Navigate to the new URL
    window.location.href = url.toString();
}

function updateBulkActions() {
    const selectedCount = $('.customer-checkbox:checked').length;
    const bulkActions = $('.bulk-actions');
    const selectedCountSpan = $('.selected-count');

    if (selectedCount > 0) {
        bulkActions.show();
        selectedCountSpan.text(`${selectedCount} selected`);
    } else {
        bulkActions.hide();
    }
}

function updateSelectAllState() {
    const totalCheckboxes = $('.customer-checkbox').length;
    const checkedCheckboxes = $('.customer-checkbox:checked').length;
    const selectAllCheckbox = $('#selectAll');

    if (checkedCheckboxes === 0) {
        selectAllCheckbox.prop('indeterminate', false).prop('checked', false);
    } else if (checkedCheckboxes === totalCheckboxes) {
        selectAllCheckbox.prop('indeterminate', false).prop('checked', true);
    } else {
        selectAllCheckbox.prop('indeterminate', true);
    }
}

function performBulkAction(action, customerIds) {
    // Show loading state
    showLoading(true);

    // Create form data
    const formData = new FormData();
    formData.append('action', action);
    formData.append('customerIds', customerIds.join(','));
    formData.append('__RequestVerificationToken', $('input[name="__RequestVerificationToken"]').val());

    // Submit bulk action
    fetch(window.location.origin + '/Customer/BulkAction', {
        method: 'POST',
        body: formData
    })
    .then(response => {
        if (response.ok) {
            return response.text();
        }
        throw new Error('Network response was not ok');
    })
    .then(data => {
        showLoading(false);
        showAlert(`Bulk action completed successfully.`, 'success');
        // Reload the page to show updated data
        setTimeout(() => {
            window.location.reload();
        }, 1000);
    })
    .catch(error => {
        showLoading(false);
        showAlert('An error occurred while performing the bulk action.', 'danger');
        console.error('Error:', error);
    });
}

function autoSaveFormData() {
    const formData = {
        householdHeadName: $('#HouseholdHeadName').val(),
        address: $('#Address').val(),
        phoneNumber: $('#PhoneNumber').val(),
        status: $('#Status').val(),
        notes: $('#Notes').val(),
        timestamp: new Date().toISOString()
    };
    
    localStorage.setItem('customerFormData', JSON.stringify(formData));
}

function loadFormData() {
    const savedData = localStorage.getItem('customerFormData');
    if (savedData) {
        try {
            const formData = JSON.parse(savedData);
            
            // Check if data is not too old (24 hours)
            const savedTime = new Date(formData.timestamp);
            const now = new Date();
            const hoursDiff = (now - savedTime) / (1000 * 60 * 60);
            
            if (hoursDiff < 24) {
                $('#HouseholdHeadName').val(formData.householdHeadName || '');
                $('#Address').val(formData.address || '');
                $('#PhoneNumber').val(formData.phoneNumber || '');
                $('#Status').val(formData.status || 'Active');
                $('#Notes').val(formData.notes || '');
                
                showAlert('Form data restored from previous session.', 'info');
            } else {
                localStorage.removeItem('customerFormData');
            }
        } catch (e) {
            localStorage.removeItem('customerFormData');
        }
    }
}

function clearForm() {
    if (confirm('Are you sure you want to clear all form data?')) {
        $('form')[0].reset();
        localStorage.removeItem('customerFormData');
        showAlert('Form cleared successfully.', 'info');
    }
}

function showAlert(message, type) {
    // Remove existing alerts
    $('.alert-dismissible').remove();
    
    // Create new alert
    const alertHtml = `
        <div class="alert alert-${type} alert-dismissible fade show" role="alert">
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
        </div>
    `;
    
    // Insert at the top of the page
    $('.customer-management, .customer-form, .customer-details').prepend(alertHtml);
    
    // Auto-dismiss after 5 seconds
    setTimeout(() => {
        $('.alert-dismissible').fadeOut();
    }, 5000);
}

function showLoading(show) {
    if (show) {
        $('body').addClass('loading');
        $('.bulk-actions').addClass('loading');
    } else {
        $('body').removeClass('loading');
        $('.bulk-actions').removeClass('loading');
    }
}

// Utility functions
function formatPhoneNumber(phone) {
    // Format phone number for display
    if (phone && phone.length === 10) {
        return phone.replace(/(\d{3})(\d{3})(\d{4})/, '($1) $2-$3');
    } else if (phone && phone.length === 11) {
        return phone.replace(/(\d{1})(\d{3})(\d{3})(\d{4})/, '$1-$2-$3-$4');
    }
    return phone;
}

function formatCurrency(amount) {
    // Format currency for display
    return new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND'
    }).format(amount);
}

function formatDate(dateString) {
    // Format date for display
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
    });
}

// Export functions for use in other scripts
window.CustomerManagement = {
    performSearch,
    updateBulkActions,
    performBulkAction,
    autoSaveFormData,
    loadFormData,
    clearForm,
    showAlert,
    showLoading,
    formatPhoneNumber,
    formatCurrency,
    formatDate
};
