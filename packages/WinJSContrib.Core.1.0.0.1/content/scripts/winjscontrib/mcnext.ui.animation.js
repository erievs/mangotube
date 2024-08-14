//you may use this code freely as long as you keep the copyright notice and don't 
// alter the file name and the namespaces
//This code is provided as is and we could not be responsible for what you are making with it
//project is available at http://winjscontrib.codeplex.com

var MCNEXT = MCNEXT || {};
MCNEXT.UI = MCNEXT.UI || {};
MCNEXT.UI.Animation = MCNEXT.UI.Animation || {};

(function (Animation) {
    Animation.fadeOut = function (hidden, duration, options) {
        options = options || {};
        var args = {
            property: "opacity",
            delay: options.delay || 0,
            duration: duration || options.duration || 167,
            timing: options.easing || "ease-in-out",
            to: 0
        };

        return WinJS.UI.executeTransition(hidden, args);
    }

    Animation.fadeIn = function (hidden, duration, options) {
        options = options || {};
        return WinJS.UI.executeTransition(
            hidden,
            {
                property: "opacity",
                delay: options.delay || 0,
                duration: duration || options.duration || 167,
                timing: options.easing || "ease-in-out",
                to: 1
            });
    }

    function staggerDelay(initialDelay, extraDelay, delayFactor, delayCap) {
        return function (i) {
            var ret = initialDelay;
            for (var j = 0; j < i; j++) {
                extraDelay *= delayFactor;
                ret += extraDelay;
            }
            if (delayCap) {
                ret = Math.min(ret, delayCap);
            }
            return ret;
        };
    }

    Animation.pageExit = function (hidden, duration, options) {
        options = options || {};
        var args = {
            property: "opacity",
            delay: options.delay || staggerDelay(5, 83, 1, 333),
            duration: duration || options.duration || 600,
            timing: options.easing || "ease-in",
            to: 0
        };

        return WinJS.UI.executeTransition(hidden, args);
    }

    Animation.enterPage = function (hidden, duration, options) {
        options = options || {};
        var args = {
            property: "opacity",
            delay: options.delay || staggerDelay(5, 83, 1, 333),
            duration: duration || options.duration || 700,
            timing: options.easing || "ease-out",
            to: 1
        };

        return WinJS.UI.executeTransition(hidden, args);
    }

    Animation.tabExitPage = function (element) {
        var offsetArray;
        
        var animationParams = {
            keyframe: "MCNEXT-tabExitPage",
            property: 'transform',
            delay: staggerDelay(5, 83, 1, 333),
            duration: 400,
            timing: "ease-in"
        }

        var promise1 = WinJS.UI.executeAnimation(element, animationParams);

        var transitionParams = {
            property: "opacity",
            delay: staggerDelay(5, 83, 1, 333),
            duration: 400,
            timing: "linear",
            from: 1,
            to: 0
        }
        var promise2 = WinJS.UI.executeTransition(element, transitionParams);
        return WinJS.Promise.join([promise1, promise2]);
    }

    Animation.tabEnterPage = function (element) {
        var offsetArray;
       
        var promise1 = WinJS.UI.executeAnimation(
            element,
            {
                keyframe: "MCNEXT-tabEnterPage",
                property: 'transform',
                delay: staggerDelay(50, 83, 1, 333),
                duration: 350,
                timing: "ease-out"
            });
        var promise2 = WinJS.UI.executeTransition(
            element,
            {
                property: "opacity",
                delay: staggerDelay(50, 83, 1, 333),
                duration: 350,
                timing: "ease-out",
                from: 0,
                to: 1
            });
        return WinJS.Promise.join([promise1, promise2]);
    }

    Animation.exitGrow = function (element, duration) {
        var offsetArray;
        
        var promise1 = WinJS.UI.executeAnimation(
            element,
            {
                keyframe: "MCNEXT-exitGrow",
                property: 'transform',
                delay: staggerDelay(0, 10, 1, 50),
                duration: duration || 550,
                timing: "ease-in"
            });
        var promise2 = WinJS.UI.executeTransition(
            element,
            {
                property: "opacity",
                delay: staggerDelay(0, 10, 1, 50),
                duration: duration || 550,
                timing: "ease-in",
                from: 1,
                to: 0
            });
        return WinJS.Promise.join([promise1, promise2]);
    }

    Animation.exitShrink = function (element, duration) {
        var offsetArray;
        
        var promise1 = WinJS.UI.executeAnimation(
            element,
            {
                keyframe: "MCNEXT-exitShrink",
                property: 'transform',
                delay: staggerDelay(10, 30, 1, 50),
                duration: duration || 400,
                timing: "ease-in"
            });
        var promise2 = WinJS.UI.executeTransition(
            element,
            {
                property: "opacity",
                delay: staggerDelay(10, 30, 1, 50),
                duration: duration || 400,
                timing: "ease-in",
                from: 1,
                to: 0
            });
        return WinJS.Promise.join([promise1, promise2]);
    }

    Animation.enterShrink = function (element, duration) {
        var offsetArray;
        
        var promise1 = WinJS.UI.executeAnimation(
            element,
            {
                keyframe: "MCNEXT-enterShrink",
                property: 'transform',
                delay: staggerDelay(50, 40, 1, 50),
                duration: duration || 450,
                timing: "ease-out"
            });
        var promise2 = WinJS.UI.executeTransition(
            element,
            {
                property: "opacity",
                delay: staggerDelay(50, 40, 1, 50),
                duration: duration || 700,
                timing: "ease-out",
                from: 0,
                to: 1
            });
        return WinJS.Promise.join([promise1, promise2]);
    }
})(MCNEXT.UI.Animation);