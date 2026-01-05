// 虚拟手柄模拟器 - 统一版本 v20250104-FixKeyM-v2
console.log('Gamepad Simulator v20250104-FixKeyM-v2 loaded');
// console.log('Server Load Status: {_loadStatusLog}'); // Removed C# interpolation
(function() {
  'use strict';
  
  // 存储原始的getGamepads函数
  const originalGetGamepads = navigator.getGamepads;
  
  // 手柄状态
  let virtualGamepad = null;
  let gamepadConnected = false;
  let keyboardHandlerActive = false;
  
  // 默认按键映射 (使用 let 允许更新)
  let keyMappings = {
    /*MAPPING_PLACEHOLDER*/
  };
  
  // 打印当前加载的按键映射 (调试用)
  console.log('Loaded key mappings keys:', Object.keys(keyMappings));
  if (keyMappings['KeyM']) {
      console.log('KeyM mapping exists:', keyMappings['KeyM']);
  } else {
      console.log('KeyM mapping NOT found in initial load');
  }

  // 更新按键映射
  function updateKeyMappings(newMappings) {
    keyMappings = newMappings;
    console.log('Key mappings updated via external call');
  }
  
  // 显式挂载到 window 对象，确保外部可调用
  window.updateKeyMappings = updateKeyMappings;
  
  // 创建虚拟手柄对象
  function createVirtualGamepad() {
    const gamepad = {
      id: 'Virtual Gamepad (Keyboard Only) (Vendor: 0000 Product: 0001)',
      index: 0,
      connected: true,
      mapping: 'standard',
      axes: [0, 0, 0, 0], // [Left Stick X, Left Stick Y, Right Stick X, Right Stick Y]
      buttons: Array(17).fill().map(() => ({ pressed: false, value: 0, touched: false })),
      timestamp: performance.now()
    };
    
    // 设置只读属性以符合Gamepad接口规范
    Object.defineProperty(gamepad, 'id', { writable: false });
    Object.defineProperty(gamepad, 'index', { writable: false });
    Object.defineProperty(gamepad, 'connected', { writable: false });
    Object.defineProperty(gamepad, 'mapping', { writable: false });
    
    return gamepad;
  }
  
  // 重写navigator.getGamepads函数
  navigator.getGamepads = function() {
    //console.log('navigator.getGamepads() called, gamepadConnected:', gamepadConnected);
    
    if (gamepadConnected && virtualGamepad) {
      const gamepads = Array(4).fill(null);
      gamepads[0] = virtualGamepad;
      //console.log('Returning virtual gamepad:', gamepads[0]);
      return gamepads;
    }
    
    const originalResult = originalGetGamepads.call(this);
    //console.log('Returning original gamepads:', originalResult);
    return originalResult;
  };
  
  // 连接虚拟手柄
  function connectGamepad() {
    if (!gamepadConnected) {
      virtualGamepad = createVirtualGamepad();
      gamepadConnected = true;
      
      // 分发手柄连接事件
      try {
        const event = new CustomEvent('gamepadconnected', { 
          detail: { gamepad: virtualGamepad },
          bubbles: true,
          cancelable: true
        });
        window.dispatchEvent(event);
        
        // 尝试标准GamepadEvent
        if (typeof GamepadEvent !== 'undefined') {
          try {
            const standardEvent = new GamepadEvent('gamepadconnected', { gamepad: virtualGamepad });
            window.dispatchEvent(standardEvent);
          } catch (e) {
            console.log('Standard GamepadEvent failed, using CustomEvent only:', e.message);
          }
        }
      } catch (e) {
        console.error('Failed to dispatch gamepad connected event:', e);
      }
      
      console.log('Virtual Gamepad connected');
      console.log('Virtual gamepad object:', virtualGamepad);
    }
  }
  
  // 断开虚拟手柄
  function disconnectGamepad() {
    if (gamepadConnected) {
      try {
        const event = new CustomEvent('gamepaddisconnected', { 
          detail: { gamepad: virtualGamepad },
          bubbles: true,
          cancelable: true
        });
        window.dispatchEvent(event);
        
        if (typeof GamepadEvent !== 'undefined') {
          try {
            const standardEvent = new GamepadEvent('gamepaddisconnected', { gamepad: virtualGamepad });
            window.dispatchEvent(standardEvent);
          } catch (e) {
            console.log('Standard GamepadEvent failed, using CustomEvent only:', e.message);
          }
        }
      } catch (e) {
        console.error('Failed to dispatch gamepad disconnected event:', e);
      }
      
      virtualGamepad = null;
      gamepadConnected = false;
      console.log('Virtual Gamepad disconnected');
    }
  }
  
  // 存储按键按下时间
  const keyPressStartTimes = {};

  // 处理键盘输入
  function handleKeyboardInput(keyCode, pressed) {
    if (!gamepadConnected || !virtualGamepad) {
      console.log('Gamepad not connected, ignoring key:', keyCode);
      return;
    }
    
    const mapping = keyMappings[keyCode];
    if (mapping) {
      // 记录按键时长
      if (pressed) {
        if (!keyPressStartTimes[keyCode]) {
          keyPressStartTimes[keyCode] = performance.now();
          //console.log(`Key ${keyCode} down`);
        }
      } else {
        if (keyPressStartTimes[keyCode]) {
          const duration = performance.now() - keyPressStartTimes[keyCode];
          console.log(`Key ${keyCode} released after ${duration.toFixed(2)}ms`);
          delete keyPressStartTimes[keyCode];
        }
      }

      //console.log('Processing key:', keyCode, 'pressed:', pressed, 'mapping:', mapping);
      
      if (mapping.type === 'button') {
        virtualGamepad.buttons[mapping.index].pressed = pressed;
        virtualGamepad.buttons[mapping.index].value = pressed ? 1 : 0;
        virtualGamepad.buttons[mapping.index].touched = pressed;
        //console.log('Button', mapping.index, 'set to:', pressed);
      } else if (mapping.type === 'axis') {
        if (pressed) {
          virtualGamepad.axes[mapping.index] = mapping.value;
        } else {
          virtualGamepad.axes[mapping.index] = 0;
        }
        //console.log('Axis', mapping.index, 'set to:', virtualGamepad.axes[mapping.index]);
      }
      
      // 更新时间戳
      virtualGamepad.timestamp = performance.now();
    } else {
      console.log('No mapping found for key:', keyCode);
    }
  }
  
  // 键盘事件处理器
  function handleKeyDown(event) {
    if (!event.repeat) {
      handleKeyboardInput(event.code, true);
    }
  }
  
  function handleKeyUp(event) {
    handleKeyboardInput(event.code, false);
  }

  // 激活键盘处理器
  function activateKeyboardHandler() {
      if (!keyboardHandlerActive) {
          window.addEventListener('keydown', handleKeyDown);
          window.addEventListener('keyup', handleKeyUp);
          keyboardHandlerActive = true;
          console.log('Keyboard handler activated');
      }
  }

  // 停用键盘处理器
  function deactivateKeyboardHandler() {
      if (keyboardHandlerActive) {
          window.removeEventListener('keydown', handleKeyDown);
          window.removeEventListener('keyup', handleKeyUp);
          keyboardHandlerActive = false;
          console.log('Keyboard handler deactivated');
      }
  }

  // 暴露 API
  window.connectGamepad = connectGamepad;
  window.disconnectGamepad = disconnectGamepad;
  window.activateKeyboardHandler = activateKeyboardHandler;
  window.deactivateKeyboardHandler = deactivateKeyboardHandler;

  // 通知 C# 脚本已加载完毕
  if (window.chrome && window.chrome.webview) {
      window.chrome.webview.postMessage('gamepad_script_ready');
  }

})();
