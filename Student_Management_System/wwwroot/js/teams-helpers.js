/**
 * Microsoft Teams Theme Manager
 * Handles theme switching and persistence
 */
const TeamsTheme = {
    THEMES: {
        LIGHT: 'light',
        DARK: 'dark'
    },

    STORAGE_KEY: 'teams-app-theme',

    /**
     * Initialize theme from localStorage or system preference
     */
    init() {
        const savedTheme = localStorage.getItem(this.STORAGE_KEY);
        
        if (savedTheme && Object.values(this.THEMES).includes(savedTheme)) {
            this.setTheme(savedTheme, false);
        } else {
            // Check system preference
            const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
            this.setTheme(prefersDark ? this.THEMES.DARK : this.THEMES.LIGHT, false);
        }

        // Listen for system theme changes
        window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
            if (!localStorage.getItem(this.STORAGE_KEY)) {
                this.setTheme(e.matches ? this.THEMES.DARK : this.THEMES.LIGHT, false);
            }
        });

        // Setup theme switcher buttons if present
        this.setupThemeSwitcher();
    },

    /**
     * Set the theme
     * @param {string} theme - Theme name (light, dark)
     * @param {boolean} save - Whether to save to localStorage (default: true)
     */
    setTheme(theme, save = true) {
        document.documentElement.setAttribute('data-theme', theme);
        
        if (save) {
            localStorage.setItem(this.STORAGE_KEY, theme);
        }

        // Update theme switcher buttons
        document.querySelectorAll('.teams-theme-btn').forEach(btn => {
            btn.classList.toggle('active', btn.dataset.theme === theme);
        });

        // Dispatch custom event
        document.dispatchEvent(new CustomEvent('themeChanged', { detail: { theme } }));
    },

    /**
     * Get current theme
     * @returns {string} Current theme name
     */
    getTheme() {
        return document.documentElement.getAttribute('data-theme') || this.THEMES.LIGHT;
    },

    /**
     * Toggle between light and dark themes
     */
    toggle() {
        const current = this.getTheme();
        const next = current === this.THEMES.LIGHT ? this.THEMES.DARK : this.THEMES.LIGHT;
        this.setTheme(next);
    },

    /**
     * Cycle through all themes
     */
    cycle() {
        const themes = Object.values(this.THEMES);
        const current = this.getTheme();
        const currentIndex = themes.indexOf(current);
        const nextIndex = (currentIndex + 1) % themes.length;
        this.setTheme(themes[nextIndex]);
    },

    /**
     * Setup click handlers for theme switcher buttons
     */
    setupThemeSwitcher() {
        document.querySelectorAll('.teams-theme-btn').forEach(btn => {
            btn.addEventListener('click', () => {
                const theme = btn.dataset.theme;
                if (theme && Object.values(this.THEMES).includes(theme)) {
                    this.setTheme(theme);
                }
            });
        });
    },

    /**
     * Reset to system preference
     */
    resetToSystem() {
        localStorage.removeItem(this.STORAGE_KEY);
        const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
        this.setTheme(prefersDark ? this.THEMES.DARK : this.THEMES.LIGHT, false);
    }
};

// Initialize on DOM ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => TeamsTheme.init());
} else {
    TeamsTheme.init();
}

/**
 * Teams UI Helpers
 */
const TeamsUI = {
    /**
     * Show a toast notification
     * @param {string} message - Message to display
     * @param {string} type - Type: 'success', 'error', 'warning', 'info'
     * @param {number} duration - Duration in ms (default: 3000)
     */
    showToast(message, type = 'info', duration = 3000) {
        const toast = document.createElement('div');
        toast.className = `teams-toast teams-toast-${type}`;
        toast.innerHTML = `
            <i class="fas fa-${this.getToastIcon(type)}"></i>
            <span>${this.escapeHtml(message)}</span>
        `;
        
        document.body.appendChild(toast);
        
        // Trigger animation
        requestAnimationFrame(() => {
            toast.classList.add('show');
        });
        
        // Remove after duration
        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => toast.remove(), 300);
        }, duration);
    },

    getToastIcon(type) {
        const icons = {
            success: 'check-circle',
            error: 'exclamation-circle',
            warning: 'exclamation-triangle',
            info: 'info-circle'
        };
        return icons[type] || icons.info;
    },

    /**
     * Escape HTML to prevent XSS
     * @param {string} text - Text to escape
     * @returns {string} Escaped text
     */
    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    },

    /**
     * Format file size
     * @param {number} bytes - Size in bytes
     * @returns {string} Formatted size
     */
    formatFileSize(bytes) {
        if (!bytes) return '0 B';
        const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
        const i = Math.floor(Math.log(bytes) / Math.log(1024));
        return `${(bytes / Math.pow(1024, i)).toFixed(1)} ${sizes[i]}`;
    },

    /**
     * Format time
     * @param {string|Date} dateString - Date to format
     * @returns {string} Formatted time
     */
    formatTime(dateString) {
        const date = new Date(dateString);
        const now = new Date();
        const isToday = date.toDateString() === now.toDateString();
        
        if (isToday) {
            return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
        }
        
        return date.toLocaleDateString([], { 
            month: 'short', 
            day: 'numeric',
            hour: '2-digit', 
            minute: '2-digit' 
        });
    },

    /**
     * Get avatar color based on username
     * @param {string} username - Username
     * @returns {string} Color hex
     */
    getAvatarColor(username) {
        const colors = [
            '#6264a7', '#8c6bb1', '#4b89b5', '#33a3a6',
            '#00a8a8', '#038387', '#7db954', '#bfd730',
            '#f8a838', '#e74856', '#c239b3', '#8764b8'
        ];
        const index = username.charCodeAt(0) % colors.length;
        return colors[index];
    },

    /**
     * Debounce function
     * @param {Function} func - Function to debounce
     * @param {number} wait - Wait time in ms
     * @returns {Function} Debounced function
     */
    debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }
};

/**
 * Font Awesome Icon Fix
 * Ensures icons are loaded correctly
 */
const IconFix = {
    /**
     * Check if Font Awesome is loaded
     * @returns {boolean}
     */
    isFontAwesomeLoaded() {
        const testIcon = document.createElement('i');
        testIcon.className = 'fas fa-check';
        testIcon.style.visibility = 'hidden';
        testIcon.style.position = 'absolute';
        document.body.appendChild(testIcon);
        
        const computed = window.getComputedStyle(testIcon, ':before');
        const content = computed.getPropertyValue('content');
        document.body.removeChild(testIcon);
        
        return content !== 'none' && content !== '';
    },

    /**
     * Load Font Awesome from CDN if not loaded
     */
    ensureLoaded() {
        if (!this.isFontAwesomeLoaded()) {
            const link = document.createElement('link');
            link.rel = 'stylesheet';
            link.href = 'https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css';
            link.integrity = 'sha512-iecdLmaskl7CVkqkXNQ/ZH/XLlvWZOJyj7Yy7tcenmpD1ypASozpmT/E0iPtmFIB46ZmdtAc9eNBvH0H/ZpiBw==';
            link.crossOrigin = 'anonymous';
            link.referrerPolicy = 'no-referrer';
            document.head.appendChild(link);
        }
    },

    /**
     * Fallback to Bootstrap Icons if Font Awesome fails
     */
    fallbackToBootstrapIcons() {
        const link = document.createElement('link');
        link.rel = 'stylesheet';
        link.href = 'https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.0/font/bootstrap-icons.css';
        document.head.appendChild(link);
        
        // Add mapping class
        document.body.classList.add('use-bootstrap-icons');
    }
};

// Ensure icons are loaded on page load
document.addEventListener('DOMContentLoaded', () => {
    IconFix.ensureLoaded();
});
