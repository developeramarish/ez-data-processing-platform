import React, { useEffect, useState } from 'react';

interface SplashScreenProps {
  onFinish?: () => void;
  duration?: number; // milliseconds, default 2500
}

const SplashScreen: React.FC<SplashScreenProps> = ({
  onFinish,
  duration = 2500
}) => {
  const [fadeOut, setFadeOut] = useState(false);

  useEffect(() => {
    // Start fade out animation 500ms before navigation
    const fadeTimer = setTimeout(() => {
      setFadeOut(true);
    }, duration - 500);

    // Navigate after full duration
    const navTimer = setTimeout(() => {
      sessionStorage.setItem('ez-splash-shown', 'true');
      if (onFinish) {
        onFinish();
      }
    }, duration);

    // Cleanup timers
    return () => {
      clearTimeout(fadeTimer);
      clearTimeout(navTimer);
    };
  }, [duration, onFinish]);

  return (
    <div
      className={`splash-screen ${fadeOut ? 'fade-out' : ''}`}
      style={{
        position: 'fixed',
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        zIndex: 9999,
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        background: 'linear-gradient(135deg, #1E3A5F 0%, #0E7490 50%, #22D3EE 100%)',
        opacity: fadeOut ? 0 : 1,
        transition: 'opacity 0.5s ease-out',
      }}
    >
      <div
        className="splash-logo-container"
        style={{
          maxWidth: '500px',
          width: '80%',
          animation: 'splashPulse 2s ease-in-out',
        }}
      >
        <img
          src="/assets/logo/ez-platform-logo.svg"
          alt="EZ Platform"
          style={{
            width: '100%',
            height: 'auto',
            filter: 'drop-shadow(0 10px 30px rgba(0, 0, 0, 0.3))',
          }}
        />
      </div>

      {/* Loading indicator */}
      <div
        style={{
          marginTop: '40px',
          fontSize: '14px',
          color: 'rgba(255, 255, 255, 0.8)',
          letterSpacing: '2px',
          textTransform: 'uppercase',
          animation: 'splashFadeIn 1s ease-in',
        }}
      >
        Loading...
      </div>
    </div>
  );
};

export default SplashScreen;
