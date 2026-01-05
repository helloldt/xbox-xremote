(function() {
    // 1. Visibility API Spoofing
    const getVisible = () => 'visible';
    const getFalse = () => false;
    const getTrue = () => true;

    try {
        Object.defineProperty(document, 'hidden', { get: getFalse, configurable: true });
        Object.defineProperty(document, 'visibilityState', { get: getVisible, configurable: true });
        Object.defineProperty(document, 'webkitHidden', { get: getFalse, configurable: true });
        Object.defineProperty(document, 'webkitVisibilityState', { get: getVisible, configurable: true });
        Object.defineProperty(document, 'hasFocus', { value: getTrue, configurable: true });
    } catch (e) {}
    
    // Spoof window properties
    try {
        Object.defineProperty(window, 'hidden', { get: getFalse, configurable: true });
        Object.defineProperty(window, 'visibilityState', { get: getVisible, configurable: true });
    } catch (e) {}

    // 2. Block Visibility and Focus Events
    const originalAddEventListener = EventTarget.prototype.addEventListener;
    try {
        const blockedEventTypes = new Set([
            'visibilitychange',
            'webkitvisibilitychange',
            'mozvisibilitychange',
            'msvisibilitychange',
            'blur',
            'focusout',
            'pagehide'
        ]);

        EventTarget.prototype.addEventListener = function(type, listener, options) {
            if (blockedEventTypes.has(type)) {
                // console.log('Blocked event listener:', type);
                return;
            }
            return originalAddEventListener.call(this, type, listener, options);
        };
    } catch (e) {}
    
    // Stop event propagation for blur/focusout
    try {
        window.addEventListener('blur', (e) => {
            e.stopImmediatePropagation();
            e.stopPropagation();
            // console.log('Blocked window blur');
        }, true);
        
        window.addEventListener('focusout', (e) => {
            e.stopImmediatePropagation();
            e.stopPropagation();
        }, true);
    } catch (e) {}
    
    // 3. Audio Context Hack (Prevent suspension)
    try {
        const AudioContext = window.AudioContext || window.webkitAudioContext;
        if (AudioContext) {
            const ctx = new AudioContext();
            const osc = ctx.createOscillator();
            const gain = ctx.createGain();
            gain.gain.value = 0.001; // Inaudible
            osc.connect(gain);
            gain.connect(ctx.destination);
            osc.start();
        }
    } catch (e) {}

    // 4. RequestAnimationFrame Hack (Run in background)
    try {
        let lastTime = 0;
        window.requestAnimationFrame = function(callback) {
            const currTime = new Date().getTime();
            const timeToCall = Math.max(0, 16 - (currTime - lastTime));
            const id = window.setTimeout(function() { callback(currTime + timeToCall); }, timeToCall);
            lastTime = currTime + timeToCall;
            return id;
        };
        window.cancelAnimationFrame = function(id) {
            clearTimeout(id);
        };
    } catch (e) {}
})();
