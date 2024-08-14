//you may use this code freely as long as you keep the copyright notice and don't 
// alter the file name and the namespaces
//This code is provided as is and we could not be responsible for what you are making with it
//project is available at http://winjscontrib.codeplex.com

//this is a blank WinJS control structure. It's intended to use as a startup for new controls

(function () {
	'use strict';
	WinJS.Namespace.define("MCNEXT.UI", {	
		MyBrandNewControl : WinJS.Class.mix(WinJS.Class.define(function ctor(element, options){
			this.element = element || document.createElement('DIV');
			options = options || {};
			this.element.winControl = this;
			this.element.classList.add('win-disposable');
			WinJS.UI.setOptions(this, options);
		},{
			someProperty: {
				get: function(){
					return this._someProperty;
				},
				set: function(val){
					this._someProperty = val;
				}
			},
			dispose : function(){
				WinJS.Utilities.disposeSubTree(this.element);
			}
		}),
		WinJS.UI.DOMEventMixin,
		WinJS.Utilities.createEventProperties("myevent"))
	});
})();