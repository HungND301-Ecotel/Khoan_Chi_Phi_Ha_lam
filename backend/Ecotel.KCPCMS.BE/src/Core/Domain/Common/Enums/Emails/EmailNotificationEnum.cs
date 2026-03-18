using System.ComponentModel;

namespace Domain.Common.Enums.Emails;

public enum EmailNotificationTemplateType
{
    [Description("Notify to SSC department when having new Lost Report")]
    NotifyNewLostReport = 1,

    [Description("Notify to academy department when having new Pending Lost Report")]
    NotifyNewPendingLostReport = 2,

    [Description("Notify to reportee when having Approved Pending Lost Report")]
    NotifyApprovedPendingLostReport = 3,

    [Description("Notify to reportee when having Rejected Pending Lost Report")]
    NotifyRejectedPendingLostReport = 4,

    [Description("Notify to handler when having Assigned Handler Lost Report")]
    NotifyAssignedHandlerLostReport = 5,

    [Description("Notify to handler when having UnAssigned Handler Lost Report")]
    NotifyUnAssignedHandlerLostReport = 6,

    [Description("Notify to investigator when having Assigned Investigator Lost Report")]
    NotifyAssignedInvestigatorLostReport = 7,

    [Description("Notify to investigator when having UnAssigned Investigator Lost Report")]
    NotifyUnAssignedInvestigatorLostReport = 8,

    [Description("Notify to handler when reportee update Item found Lost Report")]
    NotifyHandlerReporteeItemFoundLostReport = 9,

    [Description("Notify to handler when custodian update Item found Lost Report")]
    NotifyHandlerCustodianItemFoundLostReport = 10,

    [Description("Notify to reportee when handler update Item found Lost Report")]
    NotifyReporteeHandlerItemFoundLostReport = 11,

    [Description("Notify to handler when reportee closed Lost Report")]
    NotifyHandlerReporteeCloseLostReport = 12,

    [Description("Notify to reportee when handler closed Lost Report")]
    NotifyReporteeHandlerCloseLostReport = 13,

    [Description("Notify to security department when having update Investigating Lost Report")]
    NotifyInvestigatingLostReport = 14,

    [Description("Notify to hanlder when having handover item Lost Report")]
    NotifyHandlerHandoverItemLostReport = 15,

    [Description("Notify to owner when having handover item Lost Report")]
    NotifyOwnerHandoverItemLostReport = 16,
}