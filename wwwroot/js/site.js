// Authentication and cache control for preventing back-button issues after logout

// Function to check if user is still authenticated
function checkAuthStatus() {
    // Only perform the check on pages that require authentication
    if (document.body.hasAttribute('data-requires-auth')) {
        fetch('/api/auth/check', {
            method: 'GET',
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            },
            credentials: 'same-origin'
        })
        .then(response => {
            if (response.status === 401) {
                // If unauthorized, redirect to login page
                window.location.href = '/Identity/Account/Login';
            }
        })
        .catch(error => {
            console.error('Auth check failed:', error);
        });
    }
}

// Handle page show events (including back-forward cache restoration)
window.addEventListener('pageshow', function(event) {
    // If page is loaded from back-forward cache
    if (event.persisted) {
        // Force a full page reload to check authentication
        window.location.reload();
    }
});

// Set up browser history handling to prevent caching
document.addEventListener('DOMContentLoaded', function() {
    // For pages that require authentication
    if (document.body.hasAttribute('data-requires-auth')) {
        // Replace current history state with timestamped one
        window.history.replaceState(
            { timestamp: Date.now() }, 
            document.title, 
            window.location.href
        );
        
        // Check auth status on page load
        checkAuthStatus();
        
        // Periodically check auth status (every 30 seconds)
        setInterval(checkAuthStatus, 30000);
    }
    
    // Handle logout buttons - ensure proper cleanup
    const logoutForms = document.querySelectorAll('form[action*="Logout"]');
    logoutForms.forEach(form => {
        form.addEventListener('submit', function() {
            // Clear any stored state
            sessionStorage.clear();
            localStorage.setItem('logged_out', 'true');
            
            // Disable back button after logout by adding history entry
            window.history.pushState(null, '', window.location.href);
        });
    });
    
    // Check if we previously logged out
    if (localStorage.getItem('logged_out') === 'true') {
        // If we're on a protected page and previously logged out
        if (document.body.hasAttribute('data-requires-auth')) {
            window.location.href = '/Identity/Account/Login';
        }
        // If we're on login page, clear the flag
        else if (window.location.pathname.includes('/Account/Login')) {
            localStorage.removeItem('logged_out');
        }
    }
});

// Disable back button after logout
window.addEventListener('popstate', function(event) {
    if (localStorage.getItem('logged_out') === 'true') {
        if (document.body.hasAttribute('data-requires-auth')) {
            window.location.href = '/Identity/Account/Login';
        }
    }
});