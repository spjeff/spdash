<%@ Assembly Name="$SharePoint.Project.AssemblyFullName$"%>
<%@ Page Language="C#" Inherits="SPDash.ManageJob" MasterPageFile="~/_admin/admin.master" %>
<%@ Import Namespace="Microsoft.SharePoint.ApplicationPages" %>
<%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="Utilities" Namespace="Microsoft.SharePoint.Utilities" Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Import Namespace="Microsoft.SharePoint" %>
<%@ Assembly Name="Microsoft.Web.CommandUI, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="Utilities" Namespace="Microsoft.SharePoint.Utilities" Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Import Namespace="Microsoft.SharePoint" %>
<%@ Assembly Name="Microsoft.Web.CommandUI, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="wssawc" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="AdminControls" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint.ApplicationPages.Administration" %>
<%@ Register TagPrefix="wssuc" TagName="InputFormSection" src="~/_controltemplates/InputFormSection.ascx" %>
<%@ Register TagPrefix="wssuc" TagName="InputFormControl" src="~/_controltemplates/InputFormControl.ascx" %>
<%@ Register TagPrefix="wssuc" TagName="ButtonSection" src="~/_controltemplates/ButtonSection.ascx" %>
<asp:Content contentplaceholderid="PlaceHolderPageTitle" runat="server">
	<SharePoint:EncodedLiteral runat="server" text="Manage SPDash Timer Job" EncodeMethod='HtmlEncode'/>
</asp:content>
<asp:Content contentplaceholderid="PlaceHolderPageTitleInTitleArea" runat="server">
	<SharePoint:EncodedLiteral runat="server" text="Manage SPDash Timer Job" EncodeMethod='HtmlEncode'/>
</asp:Content>
<asp:content contentplaceholderid="PlaceHolderPageDescription" runat="server">
</asp:content>
<asp:content contentplaceholderid="PlaceHolderAdditionalPageHead" runat="server">

</asp:content>
<asp:content contentplaceholderid="PlaceHolderMain" runat="server">
 
         <asp:Label ID="lblMessages" runat="server" CssClass="ms-error" />

    <table border="0" cellspacing="0" cellpadding="0" width="100%">
    <tr><td>
            
        <!-- Schedule -->
        <wssuc:InputFormSection Title="Schedule" runat="server">
            <template_description>
              <SharePoint:EncodedLiteral ID="EncodedLiteral5" runat="server" text="Set the schedule for running the timer job." EncodeMethod='HtmlEncodeAllowSimpleTextFormatting'/>
            </template_description>
            <template_inputformcontrols>
              <wssuc:InputFormControl runat="server" LabelText="Schedule">
                <Template_control>
                  <asp:DropDownList id="lstSchedule" name="lstSchedule" runat="server" Width="250" >
            <asp:ListItem Text="None" />      
		    <asp:ListItem Text="Immediate" />
		    <asp:ListItem Text="Daily" />
		    <asp:ListItem Text="Weekly" />
		    <asp:ListItem Text="Monthly" />
                  </asp:DropDownList>
               </Template_control>
              </wssuc:InputFormControl>
            </template_inputformcontrols>
        </wssuc:InputFormSection>

        <!-- Buttons -->
        <wssuc:ButtonSection runat="server">
            <template_buttons>
			  <asp:Button UseSubmitBehavior="false" runat="server" class="ms-ButtonHeightWidth" OnClick="OK_Click" Text="OK" id="btnOK"/>
		</template_buttons>
        </wssuc:ButtonSection>

   </td></tr>
   </table>

   <ul>
       <li><asp:HyperLink ID="JobDetailsLink" name="JobDetailsLink" runat="server">Run Timer Job manually</asp:HyperLink></li>
   </ul>
  </asp:content>
