﻿

#pragma checksum "C:\Users\Nicholas Bryan Brook\Documents\Visual Studio 2013\Projects\ValleyTube\VideoPage.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "46AB243BC501DC7148FFEF039F5E6886"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ValleyTube
{
    partial class VideoPage : global::Windows.UI.Xaml.Controls.Page, global::Windows.UI.Xaml.Markup.IComponentConnector
    {
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 4.0.0.0")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
 
        public void Connect(int connectionId, object target)
        {
            switch(connectionId)
            {
            case 1:
                #line 17 "..\..\..\VideoPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.BackButton_Click;
                 #line default
                 #line hidden
                break;
            case 2:
                #line 115 "..\..\..\VideoPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.Button_Click_3;
                 #line default
                 #line hidden
                break;
            case 3:
                #line 104 "..\..\..\VideoPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.LoadMoreComments_Click;
                 #line default
                 #line hidden
                break;
            case 4:
                #line 57 "..\..\..\VideoPage.xaml"
                ((global::Windows.UI.Xaml.Controls.Primitives.ButtonBase)(target)).Click += this.SubscribeButton_Click;
                 #line default
                 #line hidden
                break;
            case 5:
                #line 31 "..\..\..\VideoPage.xaml"
                ((global::Windows.UI.Xaml.Controls.MediaElement)(target)).CurrentStateChanged += this.VideoPlayer_CurrentStateChanged;
                 #line default
                 #line hidden
                #line 32 "..\..\..\VideoPage.xaml"
                ((global::Windows.UI.Xaml.Controls.MediaElement)(target)).MediaEnded += this.MediaElement_MediaEnded;
                 #line default
                 #line hidden
                #line 33 "..\..\..\VideoPage.xaml"
                ((global::Windows.UI.Xaml.Controls.MediaElement)(target)).MediaOpened += this.VideoPlayer_MediaOpened;
                 #line default
                 #line hidden
                #line 34 "..\..\..\VideoPage.xaml"
                ((global::Windows.UI.Xaml.UIElement)(target)).DoubleTapped += this.VideoPlayer_DoubleTapped;
                 #line default
                 #line hidden
                break;
            case 6:
                #line 42 "..\..\..\VideoPage.xaml"
                ((global::Windows.UI.Xaml.Controls.MediaElement)(target)).MediaEnded += this.MediaElement_MediaEnded;
                 #line default
                 #line hidden
                #line 43 "..\..\..\VideoPage.xaml"
                ((global::Windows.UI.Xaml.Controls.MediaElement)(target)).MediaOpened += this.AudioPlayer_MediaOpened;
                 #line default
                 #line hidden
                break;
            }
            this._contentLoaded = true;
        }
    }
}


