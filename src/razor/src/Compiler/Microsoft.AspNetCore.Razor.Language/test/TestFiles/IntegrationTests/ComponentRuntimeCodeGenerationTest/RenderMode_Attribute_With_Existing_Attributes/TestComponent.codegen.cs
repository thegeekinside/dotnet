﻿// <auto-generated/>
#pragma warning disable 1591
namespace Test
{
    #line hidden
    using global::System;
    using global::System.Collections.Generic;
    using global::System.Linq;
    using global::System.Threading.Tasks;
    using global::Microsoft.AspNetCore.Components;
    public partial class TestComponent : global::Microsoft.AspNetCore.Components.ComponentBase
    {
        #pragma warning disable 1998
        protected override void BuildRenderTree(global::Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
        {
            __builder.OpenComponent<global::Test.TestComponent>(0);
            __builder.AddComponentParameter(1, "P2", "abc");
            global::Microsoft.AspNetCore.Components.IComponentRenderMode __renderMode = 
#nullable restore
#line 1 "x:\dir\subdir\Test\TestComponent.cshtml"
                                     Microsoft.AspNetCore.Components.Web.RenderMode.Server

#line default
#line hidden
#nullable disable
            ;
            __builder.AddComponentParameter(2, "P1", "def");
            __builder.AddComponentRenderMode(__renderMode);
            __builder.CloseComponent();
        }
        #pragma warning restore 1998
#nullable restore
#line 4 "x:\dir\subdir\Test\TestComponent.cshtml"
 
    [Parameter]public string P1 {get; set;}

    [Parameter]public string P2 {get; set;}

#line default
#line hidden
#nullable disable
    }
}
#pragma warning restore 1591
