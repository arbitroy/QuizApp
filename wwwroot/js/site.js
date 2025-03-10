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

// Add a smooth scroll to the top function
document.addEventListener('DOMContentLoaded', function() {
    // Original code from the application - Authentication check
    // Check user authentication status every minute
    function checkAuthStatus() {
        // Don't check if the user is not authenticated (no auth cookie)
        if (document.body.getAttribute('data-requires-auth') === 'true') {
            fetch('/api/auth/check')
                .then(response => {
                    if (!response.ok) {
                        // If response is not ok (401), redirect to login
                        window.location.href = '/Identity/Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname);
                    }
                })
                .catch(error => {
                    console.error('Authentication check failed:', error);
                });
        }
    }

    // Run immediately and then every minute
    setInterval(checkAuthStatus, 60000); // Check every minute

    // Add animation classes to elements as they appear in the viewport
    const animateElements = document.querySelectorAll('.animate-on-scroll');
    if (animateElements.length > 0) {
        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.classList.add('animate-fade-in');
                    observer.unobserve(entry.target);
                }
            });
        }, { threshold: 0.1 });

        animateElements.forEach(element => {
            observer.observe(element);
        });
    }

    // Enhanced form styling for quiz taking
    const questionOptions = document.querySelectorAll('.question-option');
    if (questionOptions.length > 0) {
        questionOptions.forEach(option => {
            const radio = option.querySelector('input[type="radio"]');
            
            option.addEventListener('click', function() {
                // Find all options in this question group
                const name = radio.getAttribute('name');
                const siblings = document.querySelectorAll(`input[name="${name}"]`);
                
                // Remove selected class from all options in this group
                siblings.forEach(sib => {
                    if (sib.closest('.question-option')) {
                        sib.closest('.question-option').classList.remove('selected');
                    }
                });
                
                // Add selected class to clicked option
                option.classList.add('selected');
                radio.checked = true;
            });
        });
    }

    // Enhanced countdown timer
    const countdownElement = document.getElementById('countdown');
    if (countdownElement) {
        const originalUpdateCountdown = window.updateCountdown;
        
        // Override the updateCountdown function
        window.updateCountdown = function() {
            const minutes = Math.floor(timeLeft / 60);
            const seconds = timeLeft % 60;
            
            let countdownClass = '';
            if (timeLeft <= 30) {
                countdownClass = 'danger';
            } else if (timeLeft <= 120) {
                countdownClass = 'warning';
            }
            
            // Set the countdown element classes
            countdownElement.className = 'countdown ' + countdownClass;
            
            // Format time with leading zeros
            const formattedTime = `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
            countdownElement.textContent = `Time remaining: ${formattedTime}`;
            
            if (timeLeft <= 0) {
                clearInterval(timer);
                countdownElement.textContent = "Time's up! Submitting your answers...";
                
                // Add a slight delay before submitting
                setTimeout(() => {
                    document.querySelector('form').submit();
                }, 1500);
            }
            
            timeLeft--;
        };
        
        // Call the function once to initialize
        if (typeof timeLeft !== 'undefined' && typeof timer !== 'undefined') {
            updateCountdown();
        }
    }

    // Add toast notifications
    const showToast = (message, type = 'success') => {
        // Create toast container if it doesn't exist
        let toastContainer = document.querySelector('.toast-container');
        if (!toastContainer) {
            toastContainer = document.createElement('div');
            toastContainer.className = 'toast-container position-fixed bottom-0 end-0 p-3';
            document.body.appendChild(toastContainer);
        }
        
        // Create toast element
        const toastEl = document.createElement('div');
        toastEl.className = `toast bg-${type} text-white`;
        toastEl.setAttribute('role', 'alert');
        toastEl.setAttribute('aria-live', 'assertive');
        toastEl.setAttribute('aria-atomic', 'true');
        
        // Create toast content
        toastEl.innerHTML = `
            <div class="toast-header bg-${type} text-white">
                <strong class="me-auto">QuizApp</strong>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
            <div class="toast-body">
                ${message}
            </div>
        `;
        
        // Add to container
        toastContainer.appendChild(toastEl);
        
        // Initialize and show the toast
        const toast = new bootstrap.Toast(toastEl, { 
            autohide: true,
            delay: 3000
        });
        toast.show();
        
        // Remove toast after it's hidden
        toastEl.addEventListener('hidden.bs.toast', function() {
            toastEl.remove();
        });
    };
    
    // Add to window for global access
    window.showToast = showToast;
    
    // Show toast if there's a status message
    const statusMessage = document.querySelector('[data-status-message]');
    if (statusMessage) {
        const message = statusMessage.getAttribute('data-status-message');
        const type = statusMessage.getAttribute('data-status-type') || 'success';
        
        if (message) {
            showToast(message, type);
        }
    }

    // Quiz result animation
    const resultCards = document.querySelectorAll('.result-card');
    if (resultCards.length > 0) {
        resultCards.forEach((card, index) => {
            setTimeout(() => {
                card.classList.add('animate-fade-in');
            }, 100 * index);
        });
    }

    // Quiz filter functionality
    const quizFilter = document.getElementById('quizFilter');
    if (quizFilter) {
        quizFilter.addEventListener('keyup', function() {
            const filterValue = this.value.toLowerCase();
            const quizCards = document.querySelectorAll('.quiz-card');
            
            quizCards.forEach(card => {
                const title = card.querySelector('.card-title').textContent.toLowerCase();
                const description = card.querySelector('.card-text').textContent.toLowerCase();
                
                if (title.includes(filterValue) || description.includes(filterValue)) {
                    card.style.display = 'block';
                } else {
                    card.style.display = 'none';
                }
            });
        });
    }

    // Add confetti effect for high scores
    const scoreElement = document.querySelector('.quiz-score');
    if (scoreElement) {
        const score = parseInt(scoreElement.getAttribute('data-score') || 0);
        
        if (score >= 80) {
            // Simple confetti effect
            const createConfetti = () => {
                const confetti = document.createElement('div');
                confetti.className = 'confetti';
                
                // Random position
                confetti.style.left = Math.random() * 100 + 'vw';
                
                // Random color
                const colors = ['#f72585', '#4361ee', '#4cc9f0', '#4CAF50', '#FF9800'];
                confetti.style.backgroundColor = colors[Math.floor(Math.random() * colors.length)];
                
                // Random size
                const size = Math.random() * 10 + 5;
                confetti.style.width = size + 'px';
                confetti.style.height = size + 'px';
                
                // Random rotation
                confetti.style.transform = `rotate(${Math.random() * 360}deg)`;
                
                document.body.appendChild(confetti);
                
                // Remove after animation completes
                setTimeout(() => {
                    confetti.remove();
                }, 3000);
            };
            
            // Create multiple confetti elements
            for (let i = 0; i < 100; i++) {
                setTimeout(createConfetti, i * 50);
            }
            
            // Add celebration message
            if (score >= 90) {
                showToast('Outstanding! You got an excellent score! 🏆', 'success');
            } else {
                showToast('Great job! That\'s a good score! 🎉', 'success');
            }
        }
    }
});

// Add confetti styles
const style = document.createElement('style');
style.textContent = `
    .confetti {
        position: fixed;
        top: -10px;
        width: 10px;
        height: 10px;
        z-index: 1000;
        animation: fall 3s linear forwards;
    }
    
    @keyframes fall {
        to {
            transform: translateY(100vh) rotate(720deg);
        }
    }
`;
document.head.appendChild(style);
// Disable back button after logout
window.addEventListener('popstate', function(event) {
    if (localStorage.getItem('logged_out') === 'true') {
        if (document.body.hasAttribute('data-requires-auth')) {
            window.location.href = '/Identity/Account/Login';
        }
    }
});