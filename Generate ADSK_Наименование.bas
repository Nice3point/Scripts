Attribute VB_Name = "Module1"
Option Explicit
Public RS As String
Public RG As String
Public m3h As String

Private Sub initDim()
    RS = Sheets("���������").Cells(1, 2)
    RG = Sheets("���������").Cells(2, 2)
    m3h = Sheets("���������").Cells(3, 2)
End Sub

Function Concat(data As Range) As String

    Application.Calculation = xlAutomatic

    Dim fStr As String
    Dim Group1 As New Collection '�������� ������
    Dim Group2 As New Collection '������������ ����������
    Dim Group3 As New Collection '�������������� ����������
    Dim Group4 As New Collection '����������� ����������
    Dim Group5 As New Collection '������������� ����������

    'd - ������ ������
    '� - ������ � ������������

    Dim d1 As String '�������������� ���
    Dim d2 As String '�������������� ������
    Dim d3 As String '��������
    Dim d4 As String '��������
    Dim d5 As String '��������
    Dim d6 As String '����� �������������
    Dim d7 As String 'PN
    Dim d8 As String 'DN
    Dim d9 As String '�������� ��������
    Dim d10 As String '���
    Dim d11 As String '����������� ������
    Dim d12 As String '������������ ������
    Dim d13 As String 'KVS
    Dim d14 As String '�����
    Dim d15 As String '��� �����������
    Dim d16 As String '���� �����������
    Dim d17 As String '�������� �������������
    Dim d18 As String '���
    Dim d19 As String '����
    Dim d20 As String '����������
    Dim d21 As String '������� ����

    Dim c1 As String '�������������� ���
    Dim c2 As String '�������������� ������
    Dim c3 As String '��������
    Dim c4 As String '��������
    Dim c5 As String '��������
    Dim c6 As String '����� �������������
    Dim c7 As String 'PN
    Dim c8 As String 'DN
    Dim c9 As String '�������� ��������
    Dim c10 As String '���
    Dim c11 As String '����������� ������
    Dim c12 As String '������������ ������
    Dim c13 As String 'KVS
    Dim c14 As String '�����
    Dim c15 As String '��� �����������
    Dim c16 As String '���� �����������
    Dim c17 As String '�������� �������������
    Dim c18 As String '���
    Dim c19 As String '����
    Dim c20 As String '����������
    Dim c21 As String '������� ����

    d1 = data(1)
    d2 = data(2)
    d3 = data(3)
    d4 = data(4)
    d5 = data(5)
    d6 = data(6)
    d7 = data(7)
    d8 = data(8)
    d9 = data(9)
    d10 = data(10)
    d11 = data(11)
    d12 = data(12)
    d13 = data(13)
    d14 = data(14)
    d15 = data(15)
    d16 = data(16)
    d17 = data(17)
    d18 = data(18)
    d19 = data(19)
    d20 = data(20)
    d21 = data(21)

    c1 = d1
    c2 = d2
    c3 = UCase(Left(d3, 1)) & LCase(Mid(d3, 2))
    c4 = "� �������� �������� ������: " & d4
    c5 = "�������� - " & d5
    c6 = "����� ������������� " & d6
    c7 = "PN" & d7
    c8 = "DN" & d8
    c9 = "Q = " & d9 & " ���"
    c10 = "��� = " & d10 & " %"
    c11 = "Qmin = " & d11 & m3h
    c12 = "Qmax = " & d12 & m3h
    c13 = "KVS = " & d13 & m3h
    c14 = "H = " & d14 & " �"
    c15 = "Tmin = " & d15 & " �C"
    c16 = "Tmax = " & d16 & " �C"
    c17 = "N = " & d17 & " ���"
    c18 = "I = " & d18 & " �"
    If d19 <> "" Then c19 = d19 & "~"
    c20 = d20 & " �"
    c21 = "f = " & d21 & " ��"

    Call initDim

    fStr = c1
    If d2 <> "" Then fStr = fStr & " " & c2
    If d3 <> "" Then fStr = fStr & RG & c3

    Group1.Add Array(d5, c5)
    Group1.Add Array(d6, c6)
    Group1.Add Array(d7, c7)
    Group1.Add Array(d8, c8)

    Group2.Add Array(d9, c9)
    Group2.Add Array(d10, c10)

    Group3.Add Array(d11, c11)
    Group3.Add Array(d12, c12)
    Group3.Add Array(d13, c13)
    Group3.Add Array(d14, c14)

    Group4.Add Array(d15, c15)
    Group4.Add Array(d16, c16)

    Group5.Add Array(d17, c17)
    Group5.Add Array(d18, c18)
    Group5.Add Array(d19 & d20, "U = " & c19 & c20)
    Group5.Add Array(d21, c21)

    fStr = fStr & ConcatG(Group1, RG)
    fStr = fStr & ConcatG(Group2, RG & "������������ ����������: ")
    fStr = fStr & ConcatG(Group3, RG & "�������������� ����������: ")
    fStr = fStr & ConcatG(Group4, RG & "����������� ����������: ")
    fStr = fStr & ConcatG(Group5, RG & "������������� ����������: ")

    If d4 <> "" Then fStr = fStr & RG & c4

    Concat = fStr
End Function

Private Function ConcatG(col As Collection, grName As String) As String
Dim i As Long
Dim v As Boolean
Dim f As Boolean
Dim el As Variant
Dim str
For Each el In col
    v = (el(0) <> "")
    If v = True Then
        If f = False Then
            str = str & grName & el(1)
            f = True
        Else
            str = str & RS & el(1)
        End If
    End If
Next el
ConcatG = str
End Function

