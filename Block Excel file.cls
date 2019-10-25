VERSION 1.0 CLASS
BEGIN
  MultiUse = -1  'True
END
Attribute VB_Name = "ЭтаКнига"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = True
Option Explicit

Const WelcomePage = "Macros"
Private Sub Workbook_Open()

On Error Resume Next
If Dir("\\FILE-SVR\File path") = "" Then
        Application.DisplayAlerts = False
        ActiveWorkbook.Save
        MsgBox "Отсутствует лиценция на использование файла", vbOKOnly + vbCritical, "Бог в помощь"
       ActiveWorkbook.Close
    Else
             'Unhide all worksheets
    Application.ScreenUpdating = False
    Call ShowAllSheets
    Application.ScreenUpdating = True
   End If
End Sub

Private Sub Workbook_BeforeClose(Cancel As Boolean)
     'Turn off events to prevent unwanted loops
    Application.EnableEvents = False

     'Evaluate if workbook is saved and emulate default propmts
    With ThisWorkbook
        If Not .Saved Then
            Select Case MsgBox("Do you want to save the changes you made to '" & .Name & "'?", _
                vbYesNoCancel + vbExclamation)
            Case Is = vbYes
                 'Call customized save routine
                Call CustomSave
            Case Is = vbNo
                 'Do not save
            Case Is = vbCancel
                 'Set up procedure to cancel close
                Cancel = True
            End Select
        End If

         'If Cancel was clicked, turn events back on and cancel close,
         'otherwise close the workbook without saving further changes
        If Not Cancel = True Then
            .Saved = True
            Application.EnableEvents = True
            .Close savechanges:=False
        Else
            Application.EnableEvents = True
        End If
    End With
End Sub

Private Sub Workbook_BeforeSave(ByVal SaveAsUI As Boolean, Cancel As Boolean)
     'Turn off events to prevent unwanted loops
    Application.EnableEvents = False

     'Call customized save routine and set workbook's saved property to true
     '(To cancel regular saving)
    Call CustomSave(SaveAsUI)
    Cancel = True

     'Turn events back on an set saved property to true
    Application.EnableEvents = True
    ThisWorkbook.Saved = True
End Sub


Private Sub CustomSave(Optional SaveAs As Boolean)
    Dim ws As Worksheet, aWs As Worksheet, newFname As String
     'Turn off screen flashing
    Application.ScreenUpdating = False

     'Record active worksheet
    Set aWs = ActiveSheet

     'Hide all sheets
    Call HideAllSheets

     'Save workbook directly or prompt for saveas filename
    If SaveAs = True Then
        newFname = Application.GetSaveAsFilename( _
        fileFilter:="Excel Files (*.xls), *.xls")
        If Not newFname = "False" Then ThisWorkbook.SaveAs newFname
    Else
        ThisWorkbook.Save
    End If

     'Restore file to where user was
    Call ShowAllSheets
    aWs.Activate

     'Restore screen updates
    Application.ScreenUpdating = True
End Sub

Private Sub HideAllSheets()
     'Hide all worksheets except the macro welcome page
    Dim ws As Worksheet

    Worksheets(WelcomePage).Visible = xlSheetVisible

    For Each ws In ThisWorkbook.Worksheets
        If Not ws.Name = WelcomePage Then ws.Visible = xlSheetVeryHidden
    Next ws

    Worksheets(WelcomePage).Activate
End Sub

Private Sub ShowAllSheets()
     'Show all worksheets except the macro welcome page

    Dim ws As Worksheet

    For Each ws In ThisWorkbook.Worksheets
        If Not ws.Name = WelcomePage Then ws.Visible = xlSheetVisible
    Next ws

    Worksheets(WelcomePage).Visible = xlSheetVeryHidden
End Sub
