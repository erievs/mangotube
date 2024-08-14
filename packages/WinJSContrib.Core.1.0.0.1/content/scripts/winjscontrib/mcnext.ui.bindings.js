//you may use this code freely as long as you keep the copyright notice and don't 
// alter the file name and the namespaces
//This code is provided as is and we could not be responsible for what you are making with it
//project is available at http://winjscontrib.codeplex.com

var MCNEXT;
(function (MCNEXT) {
    (function (Bindings) {
        'use strict';

        Bindings.pictureUnavailable = '/images/unavailable.png';

        function bindingArguments(elt, argname) {
            var test = JSON.stringify({
                ellipse: 30
            });
            var data = $(elt).data('win-bind-args');
            if (data) {
                if (argname) {
                    return data[argname];
                } else {
                    return data;
                }
            }
        }
        Bindings.bindingArguments = bindingArguments;

        Bindings.removeHTML = WinJS.Utilities.markSupportedForProcessing(function (source, sourceProperty, dest, destProperty) {
            var data = MCNEXT.Utils.readProperty(source, sourceProperty);
            var elt = document.createElement('DIV');
            elt.innerHTML = data;
            WinJS.Binding.oneTime({ value: elt.innerText }, ['value'], dest, [destProperty]);
        });

        Bindings.removeHTMLAndEllipsisize = WinJS.Utilities.markSupportedForProcessing(function (source, sourceProperty, dest, destProperty) {
            var data = MCNEXT.Utils.readProperty(source, sourceProperty);
            var elt = document.createElement('DIV');
            elt.innerHTML = data;
            var textRes = elt.innerText;
            var size = bindingArguments(dest, 'ellipsisize');

            if (size)
                textRes = toStaticHTML(MCNEXT.Utils.ellipsisizeString(elt.innerText, size, true));
            WinJS.Binding.oneTime({ value: textRes }, ['value'], dest, [destProperty]);
        });

        function dataAttrBinding(source, sourceProperty, dest, destProperty) {
            var data = MCNEXT.Utils.readProperty(source, sourceProperty);
            $(dest).attr('data-' + destProperty, data).data(destProperty, data);
        }
        Bindings.dataAttr = WinJS.Utilities.markSupportedForProcessing(dataAttrBinding);

        function addClassBinding(source, sourceProperty, dest, destProperty) {
            var data = MCNEXT.Utils.readProperty(source, sourceProperty);
            $(dest).addClass(data);
        }
        Bindings.addClass = WinJS.Utilities.markSupportedForProcessing(addClassBinding);

        function addClassBinding(source, sourceProperty, dest, destProperty) {
            var data = MCNEXT.Utils.readProperty(source, sourceProperty);
            $(dest).addClass(destProperty + '-' + data);
        }
        Bindings.asClass = WinJS.Utilities.markSupportedForProcessing(addClassBinding);

        function toBgImageBinding(source, sourceProperty, dest, destProperty) {
            var data = MCNEXT.Utils.readProperty(source, sourceProperty);
            if (!data || !data.length) {
                return;
            }
            dest.style.backgroundImage = 'url("' + data + '")';
        }
        Bindings.toBgImage = WinJS.Utilities.markSupportedForProcessing(toBgImageBinding);

        function ToRealDateBinding(source, sourceProperty, dest, destProperty) {
            var date = new Date(source.date * 1000);
            dest.innerHTML = date.toLocaleDateString();
        }
        Bindings.ToRealDate = WinJS.Utilities.markSupportedForProcessing(ToRealDateBinding);

        function _setPic(dest, url) {
            if (dest.nodeName.toLowerCase() == 'img') {
                dest.src = url;
            }
            else {
                dest.style.backgroundImage = 'url("' + url + '")';
            }
            dest.classList.add('imageLoaded');
        }

        function toSmartBgImageBinding(source, sourceProperty, dest, destProperty) {
            var data = MCNEXT.Utils.readProperty(source, sourceProperty);
            if (!data || !data.length) {
                _setPic(dest, Bindings.pictureUnavailable);
                return;
            }

            $(dest).removeClass('imageLoaded');
            setTimeout(function () {
                MCNEXT.Utils.loadImage(data).done(function () {
                    _setPic(dest, data);                    
                }, function () {
                    _setPic(dest, Bindings.pictureUnavailable);
                    WinJS.Utilities.addClass(dest, 'imageLoaded');
                });
            }, 250);
        }
        Bindings.toSmartBgImage = WinJS.Utilities.markSupportedForProcessing(toSmartBgImageBinding);

        function toImageBinding(source, sourceProperty, dest, destProperty) {
            var data = MCNEXT.Utils.readProperty(source, sourceProperty);
            if (!data || !data.length) {
                dest.src = Bindings.pictureUnavailable;
                return;
            }
            setTimeout(function () {
                loadImage(data).done(function () {
                    dest.src =  data;
                }, function () {
                    dest.src = Bindings.pictureUnavailable;
                });
            }, 250);
        }
        Bindings.toImageSrc = WinJS.Utilities.markSupportedForProcessing(toImageBinding);

        function obsBgImage(source, sourceProperty, dest, destProperty) {
            function setImage() {
                var data = MCNEXT.Utils.readProperty(source, sourceProperty);
                if (!data) {
                    dest.style.display = 'none';
                } else {
                    dest.style.display = '';
                    dest.style.backgroundImage = "url('" + data + "')";
                }
            }
            var bindingDesc = {
            };
            bindingDesc[sourceProperty] = setImage;
            return WinJS.Binding.bind(source, bindingDesc);
        }
        Bindings.obsBgImage = WinJS.Binding.initializer(obsBgImage);

        function hideUndefined(source, sourceProperty, dest, destProperty) {
            function setVisibility() {
                var data = MCNEXT.Utils.readProperty(source, sourceProperty);
                if (!data) {
                    if (destProperty[0] === 'opacity') {
                        dest.style.opacity = '0';
                    } else {
                        dest.style.display = 'none';
                    }
                } else {
                    if (destProperty[0] === 'opacity') {
                        dest.style.opacity = '1';
                    } else {
                        dest.style.display = '';
                    }
                }
            }
            var bindingDesc = {
            };
            bindingDesc[sourceProperty] = setVisibility;
            return WinJS.Binding.bind(source, bindingDesc);
        }
        Bindings.showIf = WinJS.Binding.initializer(hideUndefined);
        Bindings.hideIfNot = Bindings.showIf;
        Bindings.hideIfNotDefined = Bindings.showIf;//warning, deprecated

        function showUndefined(source, sourceProperty, dest, destProperty) {
            function setVisibility() {
                var data = MCNEXT.Utils.readProperty(source, sourceProperty);
                if (!data) {
                    if (destProperty === 'opacity') {
                        dest.style.opacity = '1';
                    } else {
                        dest.style.display = '';
                    }
                } else {
                    if (destProperty === 'opacity') {
                        dest.style.opacity = '0';
                    } else {
                        dest.style.display = 'none';
                    }
                }
            }
            var bindingDesc = {
            };
            bindingDesc[sourceProperty] = setVisibility;
            return WinJS.Binding.bind(source, bindingDesc);
        }
        Bindings.hideIf = WinJS.Binding.initializer(showUndefined);
        Bindings.showIfNotDefined = WinJS.Binding.initializer(showUndefined); //warning, deprecated

        function disableUndefined(source, sourceProperty, dest, destProperty) {
            function setVisibility() {
                var data = MCNEXT.Utils.readProperty(source, sourceProperty);
                if (!data) {
                    dest.disabled = true;
                } else {
                    dest.disabled = false;
                }
            }
            var bindingDesc = {
            };
            bindingDesc[sourceProperty] = setVisibility;
            return WinJS.Binding.bind(source, bindingDesc);
        }
        Bindings.enableIf = WinJS.Binding.initializer(disableUndefined);
        Bindings.disableIfNot = Bindings.enableIf;

        function enableUndefined(source, sourceProperty, dest, destProperty) {
            function setVisibility() {
                var data = MCNEXT.Utils.readProperty(source, sourceProperty);
                if (!data) {
                    dest.disabled = false;
                } else {
                    dest.disabled = true;
                }
            }
            var bindingDesc = {
            };
            bindingDesc[sourceProperty] = setVisibility;
            return WinJS.Binding.bind(source, bindingDesc);
        }
        Bindings.disableIf = WinJS.Binding.initializer(enableUndefined);
        Bindings.enableIfNot = Bindings.disableIf;

        function progressToWidth(source, sourceProperty, dest, destProperty) {
            function setWidth() {
                var data = MCNEXT.Utils.readProperty(source, sourceProperty);
                if (!data) {
                    dest.style.width = '';
                } else {
                    dest.style.width = data + '%';
                }
            }
            var bindingDesc = {
            };
            bindingDesc[sourceProperty] = setWidth;
            return WinJS.Binding.bind(source, bindingDesc);
        }
        Bindings.toWidth = WinJS.Binding.initializer(progressToWidth);

        function toEllipsisizedBinding(source, sourceProperty, dest, destProperty) {
            var sourcedata = MCNEXT.Utils.readProperty(source, sourceProperty);
            var size = bindingArguments(dest, 'ellipsisize');
            dest[destProperty] = toStaticHTML(MCNEXT.Utils.ellipsisizeString(sourcedata, size, true));
        }
        Bindings.toEllipsisized = WinJS.Utilities.markSupportedForProcessing(toEllipsisizedBinding);

    })(MCNEXT.Bindings || (MCNEXT.Bindings = {}));
    var Bindings = MCNEXT.Bindings;
})(MCNEXT || (MCNEXT = {}));
