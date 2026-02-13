/**
 * API Client — replaces Blazor ApiClient.cs
 * Centralized HTTP client with cookie auth, error handling, and auto-retry on 401
 */
(function () {
    'use strict';

    const JSON_HEADERS = { 'Content-Type': 'application/json' };
    let _isRefreshing = false;
    let _refreshQueue = [];

    function getBase() {
        return ChatApp.apiBase || '';
    }

    /**
     * Core fetch wrapper with cookie auth and error extraction
     */
    async function request(method, endpoint, data, opts) {
        opts = opts || {};
        const url = getBase() + endpoint;
        const config = {
            method: method,
            credentials: 'include',  // Always send cookies
            headers: Object.assign({}, JSON_HEADERS, opts.headers || {})
        };

        if (data !== undefined && data !== null) {
            config.body = JSON.stringify(data);
        }

        // AbortController signal support
        if (opts.signal) {
            config.signal = opts.signal;
        }

        // For file uploads — FormData, no Content-Type (browser sets boundary)
        if (opts.formData) {
            config.body = opts.formData;
            delete config.headers['Content-Type'];
        }

        let response;
        try {
            response = await fetch(url, config);
        } catch (err) {
            if (err.name === 'AbortError') {
                return { isSuccess: false, error: 'Request cancelled', aborted: true };
            }
            return { isSuccess: false, error: 'Network error. Please check your connection.' };
        }

        // Handle 401 — attempt token refresh once
        if (response.status === 401 && !opts._isRetry) {
            const refreshOk = await handleTokenRefresh();
            if (refreshOk) {
                return request(method, endpoint, data, Object.assign({}, opts, { _isRetry: true }));
            }
            // Refresh failed — redirect to login
            window.location.href = '/auth/login';
            return { isSuccess: false, error: 'Session expired. Please log in again.' };
        }

        // Success
        if (response.ok) {
            if (response.status === 204 || response.headers.get('content-length') === '0') {
                return { isSuccess: true, value: null };
            }
            try {
                const value = await response.json();
                return { isSuccess: true, value: value };
            } catch {
                return { isSuccess: true, value: null };
            }
        }

        // Error — extract message
        const errorMsg = await extractError(response);
        return { isSuccess: false, error: errorMsg, statusCode: response.status };
    }

    /**
     * Extract error message from API response
     * Matches Blazor ApiClient.ExtractErrorMessage logic
     */
    async function extractError(response) {
        try {
            const text = await response.text();
            if (!text) return getDefaultError(response.status);
            try {
                const json = JSON.parse(text);
                // ErrorResponse format
                if (json.error) return json.error;
                if (json.message) return json.message;
                // Validation errors
                if (json.errors) {
                    const messages = [];
                    for (const key in json.errors) {
                        const errs = json.errors[key];
                        if (Array.isArray(errs)) {
                            messages.push(...errs);
                        } else if (typeof errs === 'string') {
                            messages.push(errs);
                        }
                    }
                    if (messages.length > 0) return messages.join('. ');
                }
                // Title from ProblemDetails
                if (json.title) return json.title;
            } catch {
                return text.substring(0, 200);
            }
        } catch { }
        return getDefaultError(response.status);
    }

    function getDefaultError(status) {
        const defaults = {
            400: 'Bad request.',
            401: 'Unauthorized. Please log in.',
            403: 'Access denied.',
            404: 'Resource not found.',
            409: 'Conflict detected.',
            413: 'File too large.',
            422: 'Validation failed.',
            429: 'Too many requests. Please wait.',
            500: 'Server error. Please try again later.'
        };
        return defaults[status] || 'An unexpected error occurred (HTTP ' + status + ')';
    }

    /**
     * Token refresh with queue to avoid concurrent refreshes
     */
    async function handleTokenRefresh() {
        if (_isRefreshing) {
            return new Promise(function (resolve) {
                _refreshQueue.push(resolve);
            });
        }

        _isRefreshing = true;
        try {
            const resp = await fetch(getBase() + '/api/auth/refresh', {
                method: 'POST',
                credentials: 'include',
                headers: JSON_HEADERS
            });

            const success = resp.ok;

            // Resolve all queued requests
            _refreshQueue.forEach(function (resolve) { resolve(success); });
            _refreshQueue = [];

            return success;
        } catch {
            _refreshQueue.forEach(function (resolve) { resolve(false); });
            _refreshQueue = [];
            return false;
        } finally {
            _isRefreshing = false;
        }
    }

    // Public API
    ChatApp.api = {
        get: function (endpoint, opts) { return request('GET', endpoint, null, opts); },
        post: function (endpoint, data, opts) { return request('POST', endpoint, data, opts); },
        put: function (endpoint, data, opts) { return request('PUT', endpoint, data, opts); },
        del: function (endpoint, opts) { return request('DELETE', endpoint, null, opts); },
        patch: function (endpoint, data, opts) { return request('PATCH', endpoint, data, opts); },

        /**
         * Upload file(s) with FormData
         * @param {string} endpoint
         * @param {FormData} formData
         * @param {Function} onProgress - progress callback (0-100)
         * @param {Function} [xhrCallback] - optional callback that receives the XHR instance (for abort support)
         */
        upload: function (endpoint, formData, onProgress, xhrCallback) {
            return new Promise(function (resolve) {
                const xhr = new XMLHttpRequest();
                xhr.open('POST', getBase() + endpoint);
                xhr.withCredentials = true;

                // Expose XHR to caller for abort capability
                if (typeof xhrCallback === 'function') {
                    xhrCallback(xhr);
                }

                if (onProgress) {
                    xhr.upload.addEventListener('progress', function (e) {
                        if (e.lengthComputable) {
                            onProgress(Math.round((e.loaded / e.total) * 100));
                        }
                    });
                }

                xhr.onload = function () {
                    if (xhr.status >= 200 && xhr.status < 300) {
                        try {
                            resolve({ isSuccess: true, value: JSON.parse(xhr.responseText) });
                        } catch {
                            resolve({ isSuccess: true, value: null });
                        }
                    } else {
                        let errorMsg = getDefaultError(xhr.status);
                        try {
                            const json = JSON.parse(xhr.responseText);
                            if (json.error) errorMsg = json.error;
                            else if (json.message) errorMsg = json.message;
                        } catch { }
                        resolve({ isSuccess: false, error: errorMsg, statusCode: xhr.status });
                    }
                };

                xhr.onabort = function () {
                    resolve({ isSuccess: false, error: 'Upload cancelled.', aborted: true });
                };

                xhr.onerror = function () {
                    resolve({ isSuccess: false, error: 'Upload failed. Please check your connection.' });
                };

                xhr.send(formData);
            });
        },

        /**
         * Download file as blob
         */
        download: async function (endpoint, fileName) {
            try {
                const response = await fetch(getBase() + endpoint, {
                    credentials: 'include'
                });
                if (!response.ok) return { isSuccess: false, error: 'Download failed.' };
                const blob = await response.blob();
                const url = window.URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url;
                a.download = fileName || 'download';
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
                window.URL.revokeObjectURL(url);
                return { isSuccess: true };
            } catch {
                return { isSuccess: false, error: 'Download failed.' };
            }
        }
    };
})();
