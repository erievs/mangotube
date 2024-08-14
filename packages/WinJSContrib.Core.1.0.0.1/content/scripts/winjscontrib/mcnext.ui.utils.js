//you may use this code freely as long as you keep the copyright notice and don't 
// alter the file name and the namespaces
//This code is provided as is and we could not be responsible for what you are making with it
//project is available at http://winjscontrib.codeplex.com

if (!String.prototype.format) {
    String.prototype.format = function () {
        var args = arguments;
        return this.replace(/{(\d+)}/g, function (match, number) {
            return typeof args[number] != 'undefined'
              ? args[number]
              : match
            ;
        });
    };
}

if (!String.prototype.padLeft) {
    String.prototype.padLeft = function padLeft(length, leadingChar) {
        if (leadingChar === undefined) {
            leadingChar = "0";
        }

        return this.length < length ? (leadingChar + this).padLeft(length, leadingChar) : this;
    };
}

var MCNEXT = MCNEXT || {};
MCNEXT.Application = MCNEXT.Application || {};
MCNEXT.UI = MCNEXT.UI || {};

(function (MCNEXT) {
    MCNEXT.Application.debug = true;
    MCNEXT.Application.dolog = MCNEXT.Application.debug && console && console.log;

    MCNEXT.UI.EventTracker = function () {
        this.events = [];
    }
    MCNEXT.UI.EventTracker.prototype.addEvent = function (e, eventName, handler, capture) {
        e.addEventListener(eventName, handler, capture);
        this.events.push(function () {
            try {
                e.removeEventListener(eventName, handler);
            } catch (exception) {

            }
        });
    }
    MCNEXT.UI.EventTracker.prototype.addBinding = function (e, eventName, handler) {
        e.bind(eventName, handler);
        this.events.push(function () {
            e.unbind(eventName, handler);
        });
    }
    MCNEXT.UI.EventTracker.prototype.dispose = function () {
        for (var i = 0; i < this.events.length; i++) {
            this.events[i]();
        }
        this.events = [];
    }



    MCNEXT.Application._log = function (msg) {
        if (MCNEXT.Application.dolog) console.log(msg);
    };

    (function (Utils) {
        'use strict';
        String.prototype.startsWith = function (str) {
            return startsWith(this, str);
        };
        String.prototype.endsWith = function (str) {
            return endsWith(this, str);
        };

        Utils.triggerEvent = function (element, eventName, bubbles, cancellable) {
            var eventToTrigger = document.createEvent("Event");
            eventToTrigger.initEvent(eventName, bubbles, cancellable);
            element.dispatchEvent(eventToTrigger);
        };

        Utils.triggerCustomEvent = function (element, eventName, bubbles, cancellable, data) {
            var eventToTrigger = document.createEvent("CustomEvent");
            eventToTrigger.initCustomEvent(eventName, bubbles, cancellable, data);
            element.dispatchEvent(eventToTrigger);
        };

        /* 
        Core object properties features
        */

        //return object value based on property name. Property name is a string containing the name of the property, 
        //or the name of the property with an indexer, ex: myproperty[2] (to get item in a array)
        function getobject(obj, prop) {
            if (obj[prop])
                return obj[prop];

            var idx = prop.indexOf('[');
            if (idx < 0)
                return;
            var end = prop.indexOf(']', idx);
            if (end < 0)
                return;

            var val = prop.substr(idx + 1, end - idx);
            val = parseInt(val);

            return obj[val];
        }

        //set object property value based on property name. Property name is a string containing the name of the property, 
        //or the name of the property with an indexer, ex: myproperty[2] (to get item in a array)
        function setobject(obj, prop, data) {
            if (obj[prop] != undefined) {                
                if (obj.setProperty)
                    obj.setProperty(prop, data);

                obj[prop] = data;
            }

            var idx = prop.indexOf('[');
            if (idx < 0)
                return;
            var end = prop.indexOf(']', idx);
            if (end < 0)
                return;

            var val = prop.substr(idx + 1, end - idx);
            val = parseInt(val);

            obj[val] = data;
        }

        /// <signature helpKeyword="MCNEXT.Utils.readProperty">
        /// <summary locid="MCNEXT.Utils.readProperty">
        /// Read property value on an object based on expression
        /// </summary>
        /// <param name="source">
        /// the object containing data
        /// </param>
        /// <param name="properties">
        /// property descriptor. could be a string in js notation ex: 'myProp.myChildProp, 
        /// or an array of strings ['myProp', 'myChildProp']. String notation can contain indexers
        /// </param>
        /// </signature>
        function readProperty(source, properties) {
            var prop = getProperty(source, properties);
            if (prop) {
                return prop.propValue;
            }
        }
        Utils.readProperty = readProperty;

        /// <signature helpKeyword="MCNEXT.Utils.getProperty">
        /// <summary locid="MCNEXT.Utils.getProperty">
        /// return a propery descriptor for an object based on expression
        /// </summary>
        /// <param name="source">
        /// the object containing data
        /// </param>
        /// <param name="properties">
        /// property descriptor. could be a string in js notation ex: 'myProp.myChildProp, 
        /// or an array of strings ['myProp', 'myChildProp']. String notation can contain indexers
        /// </param>
        /// <returns type="{ parent: object, keyProp: object, propValue: object }" locid="MCNEXT.Utils.getProperty_returnValue">
        /// object property descriptor, containing a propValue field. Updating propValue update object property itself
        /// </returns>
        /// </signature>
        function getProperty(source, properties) {
            if (typeof properties == 'string') {
                properties = properties.split('.');
            }

            if (!properties.length) {
                return;
            }

            var parent = source;
            for (var i = 0; i < properties.length; i++) {
                if (i == properties.length - 1) {
                    return {
                        parent: parent,
                        keyProp: properties[i],
                        get propValue() {
                            return getobject(parent, this.keyProp);
                        },
                        set propValue(val) {
                            return setobject(parent, this.keyProp, val);
                        }
                    };
                }
                parent = getobject(parent, properties[i]);
            }

            return;
        }
        Utils.getProperty = getProperty;

        /// <signature helpKeyword="MCNEXT.Utils.readProperty">
        /// <summary locid="MCNEXT.Utils.readProperty">
        /// Write property value on an object based on expression
        /// </summary>
        /// <param name="source">
        /// the object containing data
        /// </param>
        /// <param name="properties">
        /// property descriptor. could be a string in js notation ex: 'myProp.myChildProp, 
        /// or an array of strings ['myProp', 'myChildProp']. String notation can contain indexers
        /// </param>
        /// <param name="data">
        /// data to feed to the property
        /// </param>
        /// </signature>
        Utils.writeProperty = function (source, properties, data) {
            var prop = getProperty(source, properties);
            if (prop) {
                prop.propValue = data;
                //prop.parent[prop.keyProp] = data;
            }
        };

        Utils.readProtocol = function (args) {
            if (args.detail.kind === Windows.ApplicationModel.Activation.ActivationKind.protocol && args.detail.uri) {
                var navArgs = { action: undefined };
                var protocolArgs = {};
                var queryargs = args.detail.uri.query;
                if (queryargs[0] == '?') {
                    queryargs = queryargs.substr(1);
                }
                if (queryargs) {
                    queryargs.split('&').forEach(function (item) {
                        var arg = item.split('=');

                        protocolArgs[arg[0]] = decodeURIComponent(arg[1]);
                    });
                }

                navArgs.protocol = {
                    action: args.detail.uri.host,
                    args: protocolArgs
                };

                return navArgs;
            }
        };

        function isConnected() {
            var nlvl = Windows.Networking.Connectivity.NetworkConnectivityLevel;
            var profile = Windows.Networking.Connectivity.NetworkInformation.getInternetConnectionProfile();
            if (profile !== null) {
                var level = profile.getNetworkConnectivityLevel();
                return level === nlvl.constrainedInternetAccess || level === nlvl.internetAccess;
            }
            return false;
        }
        Utils.isConnected = isConnected;

        function hasInternetAccess() {
            var nlvl = Windows.Networking.Connectivity.NetworkConnectivityLevel;
            var profile = Windows.Networking.Connectivity.NetworkInformation.getInternetConnectionProfile();
            if (profile !== null) {
                var level = profile.getNetworkConnectivityLevel();
                return level === nlvl.internetAccess;
            }
            return false;
        }
        Utils.hasInternetAccess = hasInternetAccess;

        function alert(opt) {
            if (opt) {
                var md = new Windows.UI.Popups.MessageDialog(opt.content);
                if (opt.title) {
                    md.title = opt.title;
                }
                if (opt.commands && opt.commands.forEach) {
                    opt.commands.forEach(function (command, index) {
                        var cmd = new Windows.UI.Popups.UICommand();
                        cmd.label = command.label;
                        if (command.callback) {
                            cmd.invoked = command.callback;
                        }
                        ((md.commands)).append(cmd);
                        if (command.isDefault) {
                            md.defaultCommandIndex = index;
                        }
                    });
                }
                return (md.showAsync());
            }
            return WinJS.Promise.wrapError("you must specify commands as an array of objects with properties text and callback such as {text: '', callback: function(c){}}");
        }
        Utils.alert = alert;

        Utils.toastNotification = function (data) {
            var notifications = Windows.UI.Notifications;
            var template = notifications.ToastTemplateType[data.template]; //toastImageAndText01;
            var toastXml = notifications.ToastNotificationManager.getTemplateContent(template);
            var toastTextElements = toastXml.getElementsByTagName("text");
            var toastImageElements = toastXml.getElementsByTagName("image");
            if (data.launch) {
                var toastElements = toastXml.getElementsByTagName("toast");
                toastElements[0].setAttribute("launch", JSON.stringify(data.launch)); //"ms-appx:///images/logo.png"
            }

            toastTextElements[0].appendChild(toastXml.createTextNode(data.text));

            if (data.text2 && toastTextElements.length > 1) {
                toastTextElements[1].appendChild(toastXml.createTextNode(data.text2));
            }

            if (data.picture) {
                toastImageElements[0].setAttribute("src", data.picture); //"ms-appx:///images/logo.png"
                //toastImageElements[0].setAttribute("alt", "red graphic");
            }

            var toast = new notifications.ToastNotification(toastXml);
            var toastNotifier = notifications.ToastNotificationManager.createToastNotifier();
            toastNotifier.show(toast);
        }

        function randomFromInterval(from, to) {
            return (Math.random() * (to - from + 1) + from) << 0;
        }
        Utils.randomFromInterval = randomFromInterval;
        function startsWith(str, strToMatch) {
            if (!strToMatch) {
                return false;
            }
            var match = (str.match("^" + strToMatch) == strToMatch);
            return match;
        }
        Utils.startsWith = startsWith;
        function endsWith(str, strToMatch) {
            if (!strToMatch) {
                return false;
            }
            return (str.match(strToMatch + "$") == strToMatch);
        }
        Utils.endsWith = endsWith;

        Utils.alphabeticSort = function (a, b) {
            if (a > b)
                return 1;
            if (a < b)
                return -1;

            return 0;
        };
        Utils.distinctArray = function (array, property, ignorecase) {
            if (array == null || array.length == 0) return null;
            if (typeof ignorecase == "undefined") ignorecase = false;
            var sMatchedItems = "";
            var foundCounter = 0;
            var newArray = [];
            if (ignorecase) {
                for (var i = 0; i < array.length; i++) {
                    if (property) {
                        var sFind = MCNEXT.Utils.readProperty(array[i], property.split('.')).toLowerCase();
                    } else {
                        var sFind = array[i];
                    }
                    if (sMatchedItems.indexOf("|" + sFind + "|") < 0) {
                        sMatchedItems += "|" + sFind + "|";
                        newArray[foundCounter++] = array[i];
                    }
                }
            } else {
                for (var i = 0; i < array.length; i++) {
                    if (property) {
                        var sFind = MCNEXT.Utils.readProperty(array[i], property.split('.'));
                    } else {
                        var sFind = array[i];
                    }

                    if (sMatchedItems.indexOf("|" + sFind + "|") < 0) {
                        sMatchedItems += "|" + sFind + "|";
                        newArray[foundCounter++] = array[i];
                    }
                }
            }
            return newArray;
        };


        Utils.getDistinctPropertyValues = function (array, property, ignorecase) {
            return Utils.distinctArray(array, property, ignorecase).map(function (item) {
                return MCNEXT.Utils.readProperty(item, property.split('.'));
            });
        };

        function removeAccents(s) {
            var r = s.toLowerCase();
            r = r.replace(new RegExp("[àáâãäå]", 'g'), "a");
            r = r.replace(new RegExp("æ", 'g'), "ae");
            r = r.replace(new RegExp("ç", 'g'), "c");
            r = r.replace(new RegExp("[èéêë]", 'g'), "e");
            r = r.replace(new RegExp("[ìíîï]", 'g'), "i");
            r = r.replace(new RegExp("ñ", 'g'), "n");
            r = r.replace(new RegExp("[òóôõö]", 'g'), "o");
            r = r.replace(new RegExp("œ", 'g'), "oe");
            r = r.replace(new RegExp("[ùúûü]", 'g'), "u");
            r = r.replace(new RegExp("[ýÿ]", 'g'), "y");
            return r;
        }
        Utils.removeAccents = removeAccents;

        function elementLoaded(elt, url) {
            return new WinJS.Promise(function (complete, error) {
                function onerror(e) {
                    elt.onload = undefined;
                    elt.onerror = undefined;
                    elt.onreadystatechange = undefined;
                    error('element not loaded');
                }
                function onload(e) {
                    elt.onload = undefined;
                    elt.onerror = undefined;
                    elt.onreadystatechange = undefined;
                    complete({
                        element: elt,
                        url: url
                    });
                }
                elt.onerror = onerror;
                elt.onload = onload;
                elt.onreadystatechange = onload;
                if (elt.naturalWidth > 0) {
                    onload(undefined);
                }
                elt.src = url;
            });
        }
        Utils.elementLoaded = elementLoaded;

        function listElementsAfterMe(elt) {
            var res = [];
            var passed = false;
            if (elt.parentElement) {
                var parent = elt.parentElement;
                for (var i = 0; i < parent.children.length; i++) {
                    if (parent.children[i] === elt) {
                        passed = true;
                    } else if (passed) {
                        res.push(parent.children[i]);
                    }
                }
            }
            return res;
        }
        Utils.listElementsAfterMe = listElementsAfterMe;

        function removeElementAnimation(elt) {
            return new WinJS.Promise(function (complete, error) {
                var remainings = listElementsAfterMe(elt);
                var anim = WinJS.UI.Animation.createDeleteFromListAnimation([
                    elt
                ], remainings);
                elt.style.position = "fixed";
                elt.style.opacity = '0';
                anim.execute().done(function () {
                    complete(elt);
                });
            });
        }
        Utils.removeElementAnimation = removeElementAnimation;

        function removePageFromHistory(pageLoc) {
            var history = [];
            if (WinJS.Navigation.history && WinJS.Navigation.history.backStack && WinJS.Navigation.history.backStack.length) {
                WinJS.Navigation.history.backStack.forEach(function (page) {
                    if (page.location !== pageLoc) {
                        history.push(page);
                    }
                });
            }
            WinJS.Navigation.history.backStack = history;
        }
        Utils.removePageFromHistory = removePageFromHistory;


        function loadImage(imgUrl) {
            return new WinJS.Promise(function (complete, error) {
                var image = new Image();
                function onerror(e) {
                    image.onload = undefined;
                    image.onerror = undefined;
                    error('image not loaded');
                }
                function onload(e) {
                    image.onload = undefined;
                    image.onerror = undefined;
                    complete({
                        element: image,
                        url: imgUrl
                    });
                }
                image.onerror = onerror;
                image.onload = onload;
                if (image.naturalWidth > 0) {
                    onload(undefined);
                }
                image.src = imgUrl;
            });
        }
        Utils.loadImage = loadImage;

        function pad2(number) {
            return (number < 10 ? '0' : '') + number;
        }
        Utils.pad2 = pad2;

        function ellipsisizeString(text, maxSize, useWordBoundary) {
            if (!text) {
                return '';
            }
            var toLong = text.length > maxSize, text_ = toLong ? text.substr(0, maxSize - 1) : text;
            text_ = useWordBoundary && toLong ? text_.substr(0, text_.lastIndexOf(' ')) : text_;
            return toLong ? text_ + '...' : text_;
        }
        Utils.ellipsisizeString = ellipsisizeString;

        Utils.removeHTML = WinJS.Utilities.markSupportedForProcessing(function (source, sourceProperty, dest, destProperty) {
            var data = MCNEXT.Utils.readProperty(source, sourceProperty);
            var elt = document.createElement('DIV');
            elt.innerHTML = data;
            WinJS.Binding.oneTime({ value: elt.innerText }, ['value'], dest, [destProperty]);
        });

        function appbarsOpen() {
            var res = document.querySelectorAll('div[data-win-control="WinJS.UI.AppBar"],div[data-win-control="WinJS.UI.NavBar"]');
            if (res && res.length) {
                for (var i = 0; i < res.length; i++) {
                    if (res[i].winControl) {
                        res[i].winControl.show();
                    }
                }
            }
        }
        Utils.appbarsOpen = appbarsOpen;

        function appbarsClose() {
            var res = document.querySelectorAll('div[data-win-control="WinJS.UI.AppBar"],div[data-win-control="WinJS.UI.NavBar"]');
            if (res && res.length) {
                for (var i = 0; i < res.length; i++) {
                    if (res[i].winControl) {
                        res[i].winControl.hide();
                    }
                }
            }
        }
        Utils.appbarsClose = appbarsClose;
        function appbarsDisable() {
            var res = document.querySelectorAll('div[data-win-control="WinJS.UI.AppBar"],div[data-win-control="WinJS.UI.NavBar"]');
            if (res && res.length) {
                for (var i = 0; i < res.length; i++) {
                    if (res[i].winControl) {
                        res[i].winControl.disabled = true;
                    }
                }
            }
        }
        Utils.appbarsDisable = appbarsDisable;
        function appbarsEnable() {
            $('div[data-win-control="WinJS.UI.AppBar"],div[data-win-control="WinJS.UI.NavBar"]').each(function () {
                if (this.winControl) {
                    this.winControl.disabled = false;
                }
            });
        }
        Utils.appbarsEnable = appbarsEnable;

        Utils.guid = function () {
            return s4() + s4() + '-' + s4() + '-' + s4() + '-' +
                   s4() + '-' + s4() + s4() + s4();

            function s4() {
                return Math.floor((1 + Math.random()) * 0x10000)
                           .toString(16)
                           .substring(1);
            }
        };

        Utils.inherit = function (element, property) {
            if (element && element.parentElement) {
                var current = element.parentElement;
                while (current) {
                    if (current.winControl) {
                        if (current.winControl[property] != undefined) {
                            return current.winControl[property];
                        }
                    }
                    current = current.parentElement;
                }
            }
        }

        Utils.getScopeControl = function (element) {
            var current = element.parentNode;

            while (current) {
                if (current.msParentSelectorScope) {
                    var scope = current.parentNode;
                    if (scope) {
                        var scopeControl = scope.winControl;
                        if (scopeControl) {
                            return scopeControl;

                        }
                        //var scopeParent = scope.parentNode;
                        //var scopeParentControl = scopeParent.winControl;
                    }
                }
                current = current.parentNode;
            }
        };

        Utils.getTemplate = function (template) {
            if (template) {
                var templatetype = typeof template;
                if (templatetype == 'string') {
                    return new WinJS.Binding.Template(null, { href: template });
                }
                if (templatetype == 'function') {
                    return {
                        render: function (data, elt) {
                            var res = template(data, elt);
                            return WinJS.Promise.as(res);
                        }
                    };
                }
                else if (template.winControl) {
                    return template.winControl;
                }
                else if (template.render) {
                    return template;
                }
            }
        }

        Utils.resolveMethod = function (element, text) {
            var res = Utils.resolveValue(element, text);
            if (res && typeof res == 'function')
                return res;

            return undefined;
        }

        Utils.readValue = function (element, text) {
            var res = Utils.resolveValue(element, text);
            if (res) {
                if (typeof res == 'function')
                    return res(element);
                else
                    return res;
            }
            return undefined;
        }

        Utils.resolveValue = function (element, text) {
            var methodName, control, method;

            if (text.indexOf('page:') == 0) {
                methodName = text.substr(5);
                control = MCNEXT.UI.Application.navigator.pageControl;
                method = MCNEXT.Utils.readProperty(control, methodName);
                if (method && typeof method == 'function')
                    method = method.bind(control);
            } else if (text.indexOf('ctrl:') == 0) {
                methodName = text.substr(5);
                control = MCNEXT.Utils.getScopeControl(element);
                method = MCNEXT.Utils.readProperty(control, methodName);
                if (method && typeof method == 'function')
                    method = method.bind(control);
            } else {
                methodName = text;
                control = MCNEXT.Utils.getScopeControl(element);
                method = MCNEXT.Utils.readProperty(window, methodName);
            }

            //if (method && typeof method == 'function')
            return method;

            //return null;
        }


        Utils.bindActions = function (element, control) {
            $('*[data-page-action]', element).each(function () {
                var actionName = $(this).data('page-action');

                var action = control[actionName];
                if (action && typeof action === 'function') {
                    $(this).tap(function (eltarg) {
                        var actionArgs = $(eltarg).data('page-action-args');
                        if (actionArgs && typeof actionArgs == 'string') {
                            try {
                                var tmp = MCNEXT.Utils.readValue(eltarg, actionArgs);
                                if (tmp) {
                                    actionArgs = tmp;
                                }
                                else {
                                    actionArgs = JSON.parse(actionArgs);
                                }
                            }
                            catch (exception) {
                                return;
                            }
                        }

                        control[actionName].bind(control)({ elt: eltarg, args: actionArgs });
                    });
                }
            });

            $('*[data-page-link]', element).each(function () {
                var target = $(this).data('page-link');

                if (target && target.indexOf('/') < 0) {
                    var tmp = MCNEXT.Utils.readProperty(window, target);
                    if (tmp) {
                        target = tmp;
                    }
                }

                if (target) {
                    $(this).tap(function (eltarg) {
                        var actionArgs = $(eltarg).data('page-action-args');
                        if (actionArgs && typeof actionArgs == 'string') {
                            try {
                                var tmp = MCNEXT.Utils.readValue(eltarg, actionArgs);
                                if (tmp) {
                                    actionArgs = tmp;
                                }
                                else {
                                    actionArgs = JSON.parse(actionArgs);
                                }
                            }
                            catch (exception) {
                                return;
                            }
                        }

                        WinJS.Navigation.navigate(target, actionArgs);
                    });
                }
            });
        }


    })(MCNEXT.Utils || (MCNEXT.Utils = {}));
    var Utils = MCNEXT.Utils;
})(MCNEXT || (MCNEXT = {}));
