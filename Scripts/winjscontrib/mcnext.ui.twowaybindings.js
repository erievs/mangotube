//you may use this code freely as long as you keep the copyright notice and don't 
// alter the file name and the namespaces
//This code is provided as is and we could not be responsible for what you are making with it
//project is available at http://winjscontrib.codeplex.com

/// <reference path="mcnext.ui.utils.js" />
var MCNEXT;
(function (MCNEXT) {
    (function (Bindings) {
        'use strict';
        
        function twoWayOnChangeBinding(source, sourceProperty, dest, destProperty) {
            function setVal() {
                var data = MCNEXT.Utils.readProperty(source, sourceProperty);
                MCNEXT.Utils.writeProperty(dest, destProperty, data);
            }

            function getVal() {
                var val = MCNEXT.Utils.getProperty(dest, destProperty);
                MCNEXT.Utils.writeProperty(source, sourceProperty, val.propValue);                
            }

            dest.addEventListener('change', getVal, false);
            if (!dest.winControl) {
                dest.classList.add('win-disposable');
                dest.winControl = {
                    dispose: function () {
                        dest.removeEventListener('change', getVal);
                    }
                }
            }

            var bindingDesc = {
            };

            bindingDesc[sourceProperty] = setVal;
            return WinJS.Binding.bind(source, bindingDesc);
        }
        Bindings.twoWayOnChange = WinJS.Binding.initializer(twoWayOnChangeBinding);

    })(MCNEXT.Bindings || (MCNEXT.Bindings = {}));
    var Bindings = MCNEXT.Bindings;
})(MCNEXT || (MCNEXT = {}));
//@ sourceMappingURL=WinJS.MCNEXT.Binding.Utils.js.map
