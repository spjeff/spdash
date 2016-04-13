<%@ Assembly Name="$SharePoint.Project.AssemblyFullName$" %>
<%@ Import Namespace="Microsoft.SharePoint.ApplicationPages" %>
<%@ Import Namespace="Microsoft.SharePoint" %>

<%@ Register TagPrefix="asp" Namespace="System.Web.UI" Assembly="System.Web.Extensions, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" %>
<%@ Assembly Name="Microsoft.Web.CommandUI, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="Utilities" Namespace="Microsoft.SharePoint.Utilities" Assembly="Microsoft.SharePoint, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register TagPrefix="wssawc" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c"%>
<%@ Register TagPrefix="wssuc" TagName="ToolBar" src="~/_controltemplates/ToolBar.ascx" %>
<%@ Register TagPrefix="wssuc" TagName="InputFormSection" src="~/_controltemplates/InputFormSection.ascx" %>
<%@ Register TagPrefix="wssuc" TagName="InputFormControl" src="~/_controltemplates/InputFormControl.ascx" %>
<%@ Register TagPrefix="wssuc" TagName="ButtonSection" src="~/_controltemplates/ButtonSection.ascx" %>

<%@ Page Language="C#" 
    AutoEventWireup="true" 
    CodeBehind="SPDash.aspx.cs"
    Inherits="SPDash.SPDash" 
    MasterPageFile="~/_admin/admin.master" %>

<asp:Content ID="PageHead" ContentPlaceHolderID="PlaceHolderAdditionalPageHead" runat="server">
</asp:Content>

<asp:Content ID="Main" ContentPlaceHolderID="PlaceHolderMain" runat="server">
	<style type="text/css">
	/* hide quicklaunch */
	#s4-leftpanel {
		display:none;
	}
	.s4-ca {
		margin-left:0px;
	}
	</style>
    <table border="0" width="100%"><tr><td>
        <asp:DropDownList ID="spdashSource" runat="server" OnSelectedIndexChanged="spdashSource_SelectedIndexChanged" AutoPostBack="true">
        </asp:DropDownList>
        </td><td align="right">
            <img src="/_layouts/images/xls16.gif" />
            <asp:HyperLink ID="spdashExportLink" runat="server">Export to Excel (CSV)</asp:HyperLink>
        </td></tr>
    <tr><td colspan="2">
        <asp:GridView ID="spdashGrid" runat="server">
        </asp:GridView>
    </td></tr>
    <tr><td colspan="2" align="right">
            <asp:Label ID="spdashLabelLastUpdated" runat="server" Text=""></asp:Label>
    </td></tr>
    </table>  
</asp:Content>

<asp:Content ID="PageTitle" ContentPlaceHolderID="PlaceHolderPageTitle" runat="server">
    SPDash
</asp:Content>

<asp:Content ID="PageTitleInTitleArea" ContentPlaceHolderID="PlaceHolderPageTitleInTitleArea" runat="server">
    SPDash
</asp:Content>

<asp:Content ID="PageDescription" ContentPlaceHolderID="PlaceHolderPageDescription" runat="server">
    View farm configuration settings.
</asp:Content>

