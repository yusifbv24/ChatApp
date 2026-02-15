import { useState  } from "react"
import './Login.css'

function Login() {
    const [email, setEmail]=useState('')
    const [password, setPassword]=useState('')
    const [rememberMe, setRememberMe]=useState(false)
    const [showPassword, setShowPassword]=useState(false)
    const [isLoading, setIsLoading]=useState(false)
    const [errorMessage, setErrorMessage]=useState('')

    const handleSubmit=async(e)=>{
        e.preventDefault()

        if(!email || !password){
            setErrorMessage('Email and password are required')
            return
        }

        setIsLoading(true)
        setErrorMessage('')

        try{
            const response=await fetch('http://localhost:7000/api/auth/login', {
                method: 'POST',
                headers: {'Content-Type': 'application/json'},
                credentials: 'include',
                body: JSON.stringify({email, password, rememberMe})
            })

            if(response.ok){
                if(rememberMe){
                    localStorage.setItem('rememberedEmail',email)
                } else{
                    localStorage.removeItem('rememberedEmail')
                }
                window.location.href='/'
            } else{
                const data=await response.json()
                setErrorMessage(data.error || 'Invalid email or password. Please try again.')
            } 
        } catch{
                setErrorMessage('An error occured during login. Please try again.')
        } finally{
            setIsLoading(false)
        }
    }

    return (
      <div className="login-layout">
        <div className="login-background">
          <div className="login-background-shapes">
            <div className="shape shape-1"></div>
            <div className="shape shape-2"></div>
            <div className="shape shape-3"></div>
          </div>
          <div className="particles">
            <div className="particle particle-1"></div>
            <div className="particle particle-2"></div>
            <div className="particle particle-3"></div>
            <div className="particle particle-4"></div>
            <div className="particle particle-5"></div>
            <div className="particle particle-6"></div>
            <div className="particle particle-7"></div>
            <div className="particle particle-8"></div>
          </div>
        </div>

        <div className="login-container">
          <div className="login-header">
            <div className="login-logo">
              <svg
                width="48"
                height="48"
                viewBox="0 0 48 48"
                fill="none"
                xmlns="http://www.w3.org/2000/svg"
              >
                <rect width="48" height="48" rx="12" fill="url(#gradient)" />
                <path
                  d="M12 18C12 16.3431 13.3431 15 15 15H33C34.6569 15 36 16.3431 36 18V28C36 29.6569 34.6569 31 33 31H20L14 35V31C12.8954 31 12 30.1046 12 29V18Z"
                  fill="white"
                  fillOpacity="0.9"
                />
                <circle cx="18" cy="23" r="1.5" fill="#6366F1" />
                <circle cx="24" cy="23" r="1.5" fill="#6366F1" />
                <circle cx="30" cy="23" r="1.5" fill="#6366F1" />
                <defs>
                  <linearGradient id="gradient" x1="0" y1="0" x2="48" y2="48">
                    <stop offset="0%" stopColor="#6366F1" />
                    <stop offset="100%" stopColor="#8B5CF6" />
                  </linearGradient>
                </defs>
              </svg>
              <h1 className="login-brand">ChatApp</h1>
            </div>
            <p className="login-tagline">Modern Team Communication</p>
          </div>

          <div className="login-card">
            <div className="login-card-header">
              <h2 className="login-card-title">Welcome Back</h2>
              <p className="login-card-subtitle">
                Sign in to continue to ChatApp
              </p>
            </div>

            <div className="login-card-body">
              <form onSubmit={handleSubmit}>
                <div className="login-form-group">
                  <label className="login-label">Email</label>
                  <div className="login-input-wrapper">
                    <span className="login-input-icon">👤</span>
                    <input
                      type="text"
                      value={email}
                      onChange={(e) => {
                        setEmail(e.target.value);
                        setErrorMessage("");
                      }}
                      className="login-input"
                      placeholder="Enter your email address"
                      autoComplete="email"
                    />
                  </div>
                </div>

                <div className="login-form-group">
                  <label className="login-label">Password</label>
                  <div className="login-input-wrapper">
                    <span className="login-input-icon">🔒</span>
                    <input
                      type={showPassword ? "text" : "password"}
                      value={password}
                      onChange={(e) => {
                        setPassword(e.target.value);
                        setErrorMessage("");
                      }}
                      className="login-input"
                      placeholder="Enter your password"
                      autoComplete="current-password"
                    />
                    <button
                        type="button"
                        className="login-input-toggle"
                        onClick={()=>setShowPassword(!showPassword)}
                        tabIndex={-1}
                    >    
                        {showPassword ? '🙈' : '👁'}
                    </button>
                  </div>
                </div>

                <div className="login-form-options">
                    <label className="login-checkbox">
                        <input
                            type="checkbox"
                            checked={rememberMe}
                            onChange={(e)=> setRememberMe(e.target.checked)}
                        />
                        <span>Remember me</span>
                    </label>
                </div>

                {errorMessage && (
                    <div className="login-error">
                        <span>⚠️</span>
                        <span>{errorMessage}</span>
                    </div>
                )}

                <button type="submit" className="login-button" disabled={isLoading}>
                    {isLoading ? (
                        <>
                        <span className="login-button-spinner"></span>
                        <span>Signing in...</span>
                        </>
                    ) : (
                        <span>Sign In</span>
                    )}
                </button>
              </form>
            </div>
          </div>

          <div className="login-footer">
            <p>&copy; 2026 ChatApp. All rights reserved.</p>
          </div>
        </div>
      </div>
    )
}

export default Login