using System;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI.Selection;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;
using System.Diagnostics;
using WForm = System.Windows.Forms;

namespace MacrosAutomation
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.DB.Macros.AddInId("64DC63C7-485A-4CB6-B14D-DEF61052866E")]
    public partial class ThisDocument
    {
        public static ICollection<ElementId> WarnElements = new List<ElementId>();

        private void Module_Startup(object sender, EventArgs e)
        {
        }

        private void Module_Shutdown(object sender, EventArgs e)
        {
        }

        #region ADSK guids

        public Guid ADSK_POS = new Guid("ae8ff999-1f22-4ed7-ad33-61503d85f0f4"); //Позиция
        public Guid ADSK_NAIMEN = new Guid("e6e0f5cd-3e26-485b-9342-23882b20eb43"); //Наименование
        public Guid ADSK_TIP = new Guid("2204049c-d557-4dfc-8d70-13f19715e46d"); //Тип,Марка
        public Guid ADSK_CODE = new Guid("2fd9e8cb-84f3-4297-b8b8-75f444e124ed"); //Код оборудования
        public Guid ADSK_ZAVOD = new Guid("a8cdbf7b-d60a-485e-a520-447d2055f351"); //Завод изготовитель
        public Guid ADSK_EDIZM = new Guid("4289cb19-9517-45de-9c02-5a74ebf5c86d"); //Единица измерения
        public Guid ADSK_COL = new Guid("8d057bb3-6ccd-4655-9165-55526691fe3a"); //Кол-во
        public Guid ADSK_MASSKG = new Guid("32989501-0d17-4916-8777-da950841c6d7"); //Масса единицы
        public Guid ADSK_MASSELKG = new Guid("5913a1f9-0b38-4364-96fe-a6f3cb7fcc68"); //Масса единицы с размерностью
        public Guid ADSK_PRIM = new Guid("a85b7661-26b0-412f-979c-66af80b4b2c3"); //Примечание

        #endregion

        #region CreatePipeSystemView

        private void CreateFilterForPipeSystem(Document _doc, ParameterElement _sysNameParam, string _systemName)
        {
            using (var tr = new Transaction(_doc, "Создание фильтра для: " + _systemName))
            {
                tr.Start();
                View view = this.ActiveView;
                IList<ElementId> categories = new List<ElementId>();
                categories.Add(new ElementId(BuiltInCategory.OST_PipeAccessory));
                categories.Add(new ElementId(BuiltInCategory.OST_PipeCurves));
                categories.Add(new ElementId(BuiltInCategory.OST_PipeFitting));
                categories.Add(new ElementId(BuiltInCategory.OST_PipeInsulations));
                categories.Add(new ElementId(BuiltInCategory.OST_PlumbingFixtures));
                categories.Add(new ElementId(BuiltInCategory.OST_FlexPipeCurves));
                categories.Add(new ElementId(BuiltInCategory.OST_PlaceHolderPipes));
                categories.Add(new ElementId(BuiltInCategory.OST_GenericModel));
                categories.Add(new ElementId(BuiltInCategory.OST_MechanicalEquipment));
                categories.Add(new ElementId(BuiltInCategory.OST_Sprinklers));
                var rule = ParameterFilterRuleFactory.CreateNotContainsRule(_sysNameParam.Id, _systemName, true);
                var epf = new ElementParameterFilter(rule);
                var ef = epf as ElementFilter;
                ParameterFilterElement filter = null;
                try
                {
                    filter = ParameterFilterElement.Create(_doc, "MACROS_Труб_" + _systemName, categories, ef);
                }
                catch (Autodesk.Revit.Exceptions.ArgumentException)
                {
                    var filter1 = new FilteredElementCollector(_doc)
                        .OfClass(typeof(ParameterFilterElement))
                        .Where(f => f.Name == "MACROS_Труб_" + _systemName)
                        .FirstOrDefault();
                    filter = filter1 as ParameterFilterElement;
                    filter.SetElementFilter(ef);
                }

                var eView = new FilteredElementCollector(_doc)
                    .OfClass(typeof(View))
                    .WhereElementIsNotElementType()
                    .Where(v => v.Name == "Схема_Труб_" + _systemName)
                    .FirstOrDefault();
                if (null == eView)
                {
                    var copyViewId = view.Duplicate(ViewDuplicateOption.Duplicate);
                    var copiedView = _doc.GetElement(copyViewId) as View;
                    copiedView.Name = "Схема_Труб_" + _systemName;
                    copiedView.AddFilter(filter.Id);
                    copiedView.SetFilterVisibility(filter.Id, false);
                }

                tr.Commit();
            }
        }

        private IList<string> GetPipeSystemNames(Document _doc)
        {
            return new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_PipingSystem)
                .WhereElementIsNotElementType()
                .Select(s => s.Name)
                .ToList();
        }

        public void CreatePipeSystemViews()
        {
            Document doc = this.Document;
            if (doc.ActiveView.ViewType != ViewType.ProjectBrowser)
            {
                using (var trg = new TransactionGroup(doc, "Копирование значений имя системы"))
                {
                    trg.Start();
                    foreach (var cat in GetPipeCategories())
                    {
                        IList<Element> elementsByCat = new FilteredElementCollector(doc)
                            .OfCategory(cat)
                            .WhereElementIsNotElementType()
                            .ToList();
                        if (elementsByCat.Count > 0) CopySystemNameValue(doc, elementsByCat);
                    }

                    trg.Assimilate();
                }

                var td = new TaskDialog("Copy views");

                td.Id = "ID_TaskDialog_Copy_Views";
                td.MainIcon = TaskDialogIcon.TaskDialogIconInformation;
                td.Title = "Создание копий видов с применением фильтра";
                td.TitleAutoPrefix = false;
                td.AllowCancellation = true;
                td.MainInstruction =
                    "Данные из параметра Имя системы для всех элементов трубопроводных систем скопированы";
                td.MainContent = "Хотите создать копии текущего вида с применением фильтров по системам?";
                td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Да, создать фильтры и виды");
                td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Нет");
                var tdRes = td.Show();
                if (tdRes == TaskDialogResult.CommandLink1)
                {
                    var sysNameParamElement = new FilteredElementCollector(doc)
                        .OfClass(typeof(ParameterElement))
                        .Where(p => p.Name == "ИмяСистемы")
                        .FirstOrDefault();
                    var sysNameParam = sysNameParamElement as ParameterElement;
                    foreach (var systemName in GetPipeSystemNames(doc))
                        CreateFilterForPipeSystem(doc, sysNameParam, systemName);
                }
            }
            else
            {
                TaskDialog.Show("Предупреждение", "Не активирован вид для создания копий с применением фильтра");
            }
        }

        private IList<BuiltInCategory> GetPipeCategories()
        {
            IList<BuiltInCategory> cats = new List<BuiltInCategory>();
            cats.Add(BuiltInCategory.OST_PipeAccessory);
            cats.Add(BuiltInCategory.OST_PipeCurves);
            cats.Add(BuiltInCategory.OST_PipeFitting);
            cats.Add(BuiltInCategory.OST_PipeInsulations);
            cats.Add(BuiltInCategory.OST_PlumbingFixtures);
            cats.Add(BuiltInCategory.OST_FlexPipeCurves);
            cats.Add(BuiltInCategory.OST_PlaceHolderPipes);
            cats.Add(BuiltInCategory.OST_MechanicalEquipment);
            cats.Add(BuiltInCategory.OST_Sprinklers);
            return cats;
        }

        #endregion

        #region CreateDuctSystemView

        private void CreateFilterForDuctSystem(Document _doc, ParameterElement _sysNameParam, string _systemName)
        {
            using (var tr = new Transaction(_doc, "Создание фильтра для: " + _systemName))
            {
                tr.Start();
                View view = this.ActiveView;
                IList<ElementId> categories = new List<ElementId>();
                categories.Add(new ElementId(BuiltInCategory.OST_DuctAccessory));
                categories.Add(new ElementId(BuiltInCategory.OST_DuctCurves));
                categories.Add(new ElementId(BuiltInCategory.OST_DuctFitting));
                categories.Add(new ElementId(BuiltInCategory.OST_DuctInsulations));
                categories.Add(new ElementId(BuiltInCategory.OST_DuctTerminal));
                categories.Add(new ElementId(BuiltInCategory.OST_FlexDuctCurves));
                categories.Add(new ElementId(BuiltInCategory.OST_PlaceHolderDucts));
                categories.Add(new ElementId(BuiltInCategory.OST_GenericModel));
                categories.Add(new ElementId(BuiltInCategory.OST_MechanicalEquipment));
                var rule = ParameterFilterRuleFactory.CreateNotContainsRule(_sysNameParam.Id, _systemName, true);
                var epf = new ElementParameterFilter(rule);
                var ef = epf as ElementFilter;
                ParameterFilterElement filter = null;
                try
                {
                    filter = ParameterFilterElement.Create(_doc, "MACROS_Возд_" + _systemName, categories, ef);
                }
                catch (Autodesk.Revit.Exceptions.ArgumentException)
                {
                    var filter1 = new FilteredElementCollector(_doc)
                        .OfClass(typeof(ParameterFilterElement))
                        .Where(f => f.Name == "MACROS_Возд_" + _systemName)
                        .First();
                    filter = filter1 as ParameterFilterElement;
                    filter.SetElementFilter(ef);
                }

                var eView = new FilteredElementCollector(_doc)
                    .OfClass(typeof(View))
                    .WhereElementIsNotElementType()
                    .Where(v => v.Name == "Схема_Возд_" + _systemName)
                    .FirstOrDefault();
                if (null == eView)
                {
                    var copyViewId = view.Duplicate(ViewDuplicateOption.Duplicate);
                    var copiedView = _doc.GetElement(copyViewId) as View;
                    copiedView.Name = "Схема_Возд__" + _systemName;
                    copiedView.AddFilter(filter.Id);
                    copiedView.SetFilterVisibility(filter.Id, false);
                }

                tr.Commit();
            }
        }

        private IList<string> GetDuctSystemNames(Document _doc)
        {
            return new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_DuctSystem)
                .WhereElementIsNotElementType()
                .Select(s => s.Name)
                .ToList();
        }

        public void CreateDuctSystemViews()
        {
            Document doc = this.Document;
            if (doc.ActiveView.ViewType != ViewType.ProjectBrowser)
            {
                using (var trg = new TransactionGroup(doc, "Копирование значений имя системы"))
                {
                    trg.Start();
                    foreach (var cat in GetDuctCategories())
                    {
                        IList<Element> elementsByCat = new FilteredElementCollector(doc)
                            .OfCategory(cat)
                            .WhereElementIsNotElementType()
                            .ToList();
                        if (elementsByCat.Count > 0) CopySystemNameValue(doc, elementsByCat);
                    }

                    trg.Assimilate();
                }

                var td = new TaskDialog("Copy views");

                td.Id = "ID_TaskDialog_Copy_Views";
                td.MainIcon = TaskDialogIcon.TaskDialogIconInformation;
                td.Title = "Создание копий видов с применением фильтра";
                td.TitleAutoPrefix = false;
                td.AllowCancellation = true;
                td.MainInstruction =
                    "Данные из параметра Имя системы для всех элементов систем воздуховодов скопированы";
                td.MainContent = "Хотите создать копии текущего вида с применением фильтров по системам?";
                td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Да, создать фильтры и виды");
                td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Нет");
                var tdRes = td.Show();
                if (tdRes == TaskDialogResult.CommandLink1)
                {
                    var sysNameParamElement = new FilteredElementCollector(doc)
                        .OfClass(typeof(ParameterElement))
                        .Where(p => p.Name == "ИмяСистемы")
                        .FirstOrDefault();
                    var sysNameParam = sysNameParamElement as ParameterElement;
                    foreach (var systemName in GetDuctSystemNames(doc))
                        CreateFilterForDuctSystem(doc, sysNameParam, systemName);
                }
            }
            else
            {
                TaskDialog.Show("Предупреждение", "Не активирован вид для создания копий с применением фильтра");
            }
        }

        private void CopySystemNameValue(Document _doc, IList<Element> _elements)
        {
            using (var tr = new Transaction(_doc, "CopyNames"))
            {
                tr.Start();
                foreach (var curElement in _elements)
                {
                    var rbs_name = curElement.get_Parameter(BuiltInParameter.RBS_SYSTEM_NAME_PARAM).AsString();
                    var fInstance = curElement as FamilyInstance;
                    if (null != fInstance)
                    {
                        if (null != fInstance.SuperComponent)
                        {
                            rbs_name = fInstance.SuperComponent.get_Parameter(BuiltInParameter.RBS_SYSTEM_NAME_PARAM)
                                .AsString();
                            fInstance.LookupParameter("ИмяСистемы").Set(rbs_name);
                        }
                        else
                        {
                            fInstance.LookupParameter("ИмяСистемы").Set(rbs_name);
                        }
                    }
                    else
                    {
                        curElement.LookupParameter("ИмяСистемы").Set(rbs_name);
                    }
                }

                tr.Commit();
            }
        }

        private IList<BuiltInCategory> GetDuctCategories()
        {
            IList<BuiltInCategory> cats = new List<BuiltInCategory>();
            cats.Add(BuiltInCategory.OST_DuctAccessory);
            cats.Add(BuiltInCategory.OST_DuctCurves);
            cats.Add(BuiltInCategory.OST_DuctFitting);
            cats.Add(BuiltInCategory.OST_DuctInsulations);
            cats.Add(BuiltInCategory.OST_DuctTerminal);
            cats.Add(BuiltInCategory.OST_FlexDuctCurves);
            cats.Add(BuiltInCategory.OST_PlaceHolderDucts);
            cats.Add(BuiltInCategory.OST_MechanicalEquipment);
            return cats;
        }

        #endregion

        #region CopyToADSK macros

        public void CopyParameters()
        {
            Document doc = this.Document;
            IList<string> viewSchedules = new List<string>();
            viewSchedules.Add("В_ТМ_Гибкие воздуховоды");
            viewSchedules.Add("В_ТМ_Изоляция воздуховодов");
            viewSchedules.Add("В_ТМ_Круглые воздуховоды");
            viewSchedules.Add("В_ТМ_Прямоугольные воздуховоды");
            viewSchedules.Add("В_ТМ_Фасонные детали воздуховодов");
            viewSchedules.Add("В_ТМ_Гибкие трубы");
            viewSchedules.Add("В_ТМ_Изоляция труб");
            viewSchedules.Add("В_ТМ_Трубопроводы");
            viewSchedules.Add("В_ТМ_Технико-экономические показатели");
            foreach (var vSched in viewSchedules)
            {
                var curViewSchedule = new FilteredElementCollector(doc)
                    .OfClass(typeof(ViewSchedule))
                    .FirstOrDefault(vs => vs.Name.Equals(vSched)) as ViewSchedule;
                if (null != curViewSchedule)
                {
                    this.ActiveView = curViewSchedule;
                    CopyToADSK(doc, curViewSchedule);
                    foreach (UIView uiView in this.Application.ActiveUIDocument.GetOpenUIViews())
                        if (curViewSchedule.Id == uiView.ViewId)
                            uiView.Close();
                }
            }
        }

        private void CopyToADSK(Document doc, ViewSchedule vs)
        {
            var copyLenght = false;
            var copyVolume = false;
            var copyArea = false;
            var copyComment = false;
            var copyMass = false;
            var copyValue = true;
            var copyTemperature = false;
            if ((vs.Definition.CategoryId.IntegerValue == new ElementId(BuiltInCategory.OST_PipeCurves).IntegerValue) |
                (vs.Definition.CategoryId.IntegerValue == new ElementId(BuiltInCategory.OST_DuctCurves).IntegerValue) |
                (vs.Definition.CategoryId.IntegerValue ==
                 new ElementId(BuiltInCategory.OST_FlexDuctCurves).IntegerValue) |
                (vs.Definition.CategoryId.IntegerValue ==
                 new ElementId(BuiltInCategory.OST_FlexPipeCurves).IntegerValue))
            {
                copyLenght = true;
                copyComment = true;
            }

            if (vs.Definition.CategoryId.IntegerValue == new ElementId(BuiltInCategory.OST_PipeCurves).IntegerValue)
                copyTemperature = true;

            if (vs.Definition.CategoryId.IntegerValue == new ElementId(BuiltInCategory.OST_PipeInsulations).IntegerValue
            )
                copyVolume = true;

            if (vs.Definition.CategoryId.IntegerValue ==
                new ElementId(BuiltInCategory.OST_DuctInsulations).IntegerValue)
                copyArea = true;

            if (vs.Name.Equals("В_ТМ_Технико-экономические показатели"))
            {
                copyMass = true;
                copyValue = false;
            }

            var tData = vs.GetTableData();
            var tsDada = tData.GetSectionData(SectionType.Body);
            var dataValue = "";
            var commentValue = "";
            using (var tGroup =
                new TransactionGroup(doc, "Заполнение данных ADSK_Наименование для спецификации: " + vs.Name))
            {
                tGroup.Start();
                for (var rInd = 0; rInd < tsDada.NumberOfRows; rInd++)
                {
                    var elementsOnRow = GetElementsOnRow(doc, vs, rInd);
                    if (null != elementsOnRow)
                    {
                        try
                        {
                            dataValue = tsDada.GetCellText(rInd, 1);
                            commentValue = tsDada.GetCellText(rInd, tsDada.LastColumnNumber);
                        }
                        catch (Exception ex)
                        {
                            TaskDialog.Show("Ошибка",
                                vs.Name + "\nInd: " + rInd + " of " + tsDada.NumberOfRows + "\n" + ex.Message);
                            return;
                        }

                        if (copyValue) SetValue(doc, dataValue, elementsOnRow);

                        if (copyLenght) CopyLenghtValue(doc, copyComment, commentValue, elementsOnRow);

                        if (copyMass) CopyMass(doc, dataValue, elementsOnRow);
                        
                        if (copyTemperature) CopyTemperature(doc, elementsOnRow);

                        if (copyVolume) CopyVolumeValue(doc, elementsOnRow);

                        if (copyArea) CopyAreaValue(doc, elementsOnRow);
                    }
                }

                tGroup.Assimilate();
            }
        }

        private static double GetPercentGlobal(Document doc)
        {
            double percent = 0;
            var percentValue = new FilteredElementCollector(doc).OfClass(typeof(GlobalParameter))
                .Cast<GlobalParameter>()
                .FirstOrDefault(gp => gp.Name.Equals("Спецификация_ЗапасДлины"));
            var dVal = percentValue.GetValue() as DoubleParameterValue;
            percent = dVal.Value;
            return percent;
        }

        private void SetValue(Document doc, string valueData, List<Element> elements)
        {
            using (var tr = new Transaction(doc, "Заполнение значений ADSK_Наименование"))
            {
                tr.Start();
                foreach (var curElement in elements)
                    curElement.get_Parameter(ADSK_NAIMEN).Set(valueData); // Заполнение параметра ADSK_Наименование

                tr.Commit();
            }
        }

        private void CopyLenghtValue(Document doc, bool copyComm, string commentValue, List<Element> elements)
        {
            using (var tr = new Transaction(doc, "Заполнение значений ADSK_Количество и ADSK_Примечание"))
            {
                tr.Start();
                foreach (var curElement in elements)
                {
                    var len = curElement.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble();
                    len = UnitUtils.ConvertFromInternalUnits(len, DisplayUnitType.DUT_METERS) * GetPercentGlobal(doc);
                    var fp = curElement as FlexPipe;
                    if (null != fp)
                    {
                        var fpt = doc.GetElement(fp.GetTypeId()) as FlexPipeType;
                        if (null != fpt && fpt.LookupParameter("Тип трубопровода").AsDouble() == 3)
                            curElement.get_Parameter(ADSK_COL)
                                .Set(1.0); // Заполнение параметра ADSK_Количество для гибкой трубы - Гибкая подводка.
                        else
                            curElement.get_Parameter(ADSK_COL)
                                .Set(len); // Заполнение параметра ADSK_Количество для любых других гибких труб.
                    }
                    else
                    {
                        curElement.get_Parameter(ADSK_COL)
                            .Set(len); // Заполнение параметра ADSK_Количество для линейных элементов, трубы и воздуховоды
                    }

                    if (copyComm)
                        curElement.get_Parameter(ADSK_PRIM)
                            .Set(commentValue); // Заполнение параметра ADSK_Примечание, площадь для воздуховодов
                }

                tr.Commit();
            }
        }

        private void CopyVolumeValue(Document doc, List<Element> elements)
        {
            using (var tr = new Transaction(doc, "Заполнение значений ADSK_Количество"))
            {
                tr.Start();
                foreach (var curElement in elements)
                {
                    var len = curElement.get_Parameter(BuiltInParameter.RBS_INSULATION_LINING_VOLUME).AsDouble();
                    len = UnitUtils.ConvertFromInternalUnits(len, DisplayUnitType.DUT_CUBIC_METERS);
                    curElement.get_Parameter(ADSK_COL).Set(len); // Заполнение параметра ADSK_Количество
                }

                tr.Commit();
            }
        }

        private void CopyAreaValue(Document doc, List<Element> elements)
        {
            using (var tr = new Transaction(doc, "Заполнение значений ADSK_Количество"))
            {
                tr.Start();
                foreach (var curElement in elements)
                {
                    var len = curElement.get_Parameter(BuiltInParameter.RBS_CURVE_SURFACE_AREA).AsDouble();
                    len = UnitUtils.ConvertFromInternalUnits(len, DisplayUnitType.DUT_SQUARE_METERS);
                    curElement.get_Parameter(ADSK_COL).Set(len); // Заполнение параметра ADSK_Количество
                }

                tr.Commit();
            }
        }

        private void CopyMass(Document doc, string valueData, List<Element> elements)
        {
            using (var tr = new Transaction(doc, "Заполнение значений ADSK_Масса элемента"))
            {
                tr.Start();
                foreach (var curElement in elements)
                    try
                    {
                        curElement.get_Parameter(ADSK_MASSELKG)
                            .Set(valueData); // Заполнение параметра ADSK_Масса элемента
                    }
                    catch (Exception e)
                    {
                        curElement.get_Parameter(ADSK_MASSELKG).Set(valueData.Replace(',', '.'));
                    }

                tr.Commit();
            }
        }

        private void CopyTemperature(Document doc, List<Element> elements)
        {
            using (var tr = new Transaction(doc, "Заполнение значений Температура трубопровода"))
            {
                tr.Start();
                foreach (var curElement in elements)
                {
                    var pipe = curElement as Pipe;
                    if (null == pipe) continue;
                    PipingSystemType systemType;
                    systemType = doc.GetElement(pipe.MEPSystem.GetTypeId()) as PipingSystemType;
                    var temperature = systemType.FluidTemperature;
                    curElement.LookupParameter("Температура трубопровода")
                        .Set(UnitUtils.ConvertFromInternalUnits(temperature, DisplayUnitType.DUT_KELVIN));
                }

                tr.Commit();
            }
        }

        private List<Element> GetElementsOnRow(Document doc, ViewSchedule vs, int rowNumber)
        {
            var tableData = vs.GetTableData();
            var tableSectionData = tableData.GetSectionData(SectionType.Body);
            var elemIds = new FilteredElementCollector(doc, vs.Id).ToElementIds().ToList();
            var elementsOnRow = new List<Element>();
            List<ElementId> remainingElementsIds = null;

            using (var t = new Transaction(doc, "Empty"))
            {
                t.Start();
                using (var st = new SubTransaction(doc))
                {
                    st.Start();
                    try
                    {
                        tableSectionData.RemoveRow(rowNumber);
                    }
                    catch (Exception)
                    {
                        return null;
                    }

                    st.Commit();
                }

                remainingElementsIds = new FilteredElementCollector(doc, vs.Id).ToElementIds().ToList();
                t.RollBack();
            }

            foreach (var id in elemIds)
            {
                if (remainingElementsIds.Contains(id)) continue;
                elementsOnRow.Add(doc.GetElement(id));
            }

            return elementsOnRow;
        }

        #endregion

        #region Space Creation macros

        #region Classes

        public class RoomsData
        {
            public Level RoomsLevel { get; set; }
            public Level UpperRoomLevel { get; set; }
            public List<Room> RoomsList { get; set; }

            public RoomsData()
            {
            }

            public RoomsData(Level level, Level upperRoomLevel, List<Room> roomsList)
            {
                RoomsLevel = level;
                RoomsList = roomsList;
                UpperRoomLevel = upperRoomLevel;
            }

            public Level GetUpperLevel()
            {
                return UpperRoomLevel;
            }
        }

        public class SpacesData
        {
            public Level SpacesLevel { get; set; }
            public List<Space> SpacesList { get; set; }

            public SpacesData()
            {
            }

            public SpacesData(Level level, List<Space> spacesList)
            {
                SpacesLevel = level;
                SpacesList = spacesList;
            }
        }

        public class LevelsData
        {
            public string LevelName { get; set; }
            public double LevelElevation { get; set; }
            public int ElementsCount { get; set; }

            public LevelsData()
            {
            }

            public LevelsData(string levelName, double levelElevation, int elementCount)
            {
                LevelName = levelName;
                LevelElevation = levelElevation;
                ElementsCount = elementCount;
            }
        }

        #endregion

        #region Methods for Classes

        private List<RoomsData> GetRooms(Document linkedDoc)
        {
            var arRooms = new List<RoomsData>();
            var arLevels = GetLevels(linkedDoc).OrderBy(l => l.Elevation).ToList();
            for (var i = 0; i < arLevels.Count; i++)
            {
                var roomsInLevel = GetRoomsByLevel(linkedDoc, arLevels[i]);
                if (roomsInLevel.Count > 0)
                {
                    var upperLevel = arLevels[i];
                    var next_level = i + 1;
                    while (next_level < arLevels.Count &&
                           GetRoomsByLevel(linkedDoc, arLevels[next_level]).Count == 0)
                        next_level++;

                    if (next_level < arLevels.Count) upperLevel = arLevels[next_level];

                    arRooms.Add(new RoomsData(arLevels[i], upperLevel, roomsInLevel));
                }
            }

            return arRooms;
        }

        private List<SpacesData> GetSpaces(Document currentDoc)
        {
            var mepSpaces = new List<SpacesData>();
            var curLevels = GetLevels(currentDoc);
            foreach (var curLevel in curLevels)
            {
                var spacesInLevel = GetSpacesByLevel(currentDoc, curLevel);
                if (spacesInLevel.Count > 0) mepSpaces.Add(new SpacesData(curLevel, spacesInLevel));
            }

            return mepSpaces;
        }

        private List<Room> GetRoomsByLevel(Document _doc, Level _level)
        {
            return new FilteredElementCollector(_doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_Rooms)
                .Cast<Room>()
                .Where(e => e.Level.Id.IntegerValue.Equals(_level.Id.IntegerValue) && e.Area > 0)
                .ToList();
        }

        private List<Space> GetSpacesByLevel(Document _doc, Level _level)
        {
            return new FilteredElementCollector(_doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_MEPSpaces)
                .Cast<Space>()
                .Where(e => e.Level.Id.IntegerValue.Equals(_level.Id.IntegerValue) && e.Volume != 0)
                .ToList();
        }

        private List<Level> GetLevels(Document _doc)
        {
            return new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_Levels)
                .WhereElementIsNotElementType()
                .Cast<Level>()
                .ToList();
        }

        private List<LevelsData> MatchLevels(List<Level> linkedLevelList)
        {
            var levelsData = new List<LevelsData>();
            foreach (var checkedLevel in linkedLevelList)
                if (GetRoomsByLevel(linkedDOC, checkedLevel).Count() > 0)
                    if (null == GetLevelByElevation(DOC, checkedLevel.Elevation))
                        levelsData.Add(new LevelsData(checkedLevel.Name, checkedLevel.Elevation,
                            GetRoomsByLevel(linkedDOC, checkedLevel).Count()));

            return levelsData;
        }

        private bool CreateLevels(List<LevelsData> elevList)
        {
            var res = false;
            using (var trLevels = new Transaction(DOC, "Создание уровней"))
            {
                trLevels.Start();
                foreach (var lData in elevList)
                    using (var sLevel = new SubTransaction(DOC))
                    {
                        sLevel.Start();
                        var newLevel = Level.Create(DOC, lData.LevelElevation);
                        newLevel.Name =
                            "АР_" + lData
                                .LevelName; //"АР_"+UnitUtils.ConvertFromInternalUnits(elevation, DisplayUnitType.DUT_MILLIMETERS);
                        sLevel.Commit();
                        res = true;
                    }

                trLevels.Commit();
            }

            return res;
        }

        #endregion

        #region SelectionFIlter HelperClass and Method for selecting RvtLinkInstances

        public class RvtLinkInstanceFilter : ISelectionFilter
        {
            public bool AllowElement(Element element)
            {
                if (element is RevitLinkInstance) return true;

                return false;
            }

            public bool AllowReference(Reference refer, XYZ point)
            {
                return false;
            }
        }

        private Reference GetARLinkReference()
        {
            Selection arSelection = this.Application.ActiveUIDocument.Selection;
            try
            {
                return arSelection.PickObject(ObjectType.Element, new RvtLinkInstanceFilter(),
                    "Выберите экземпляр размещенной связи АР");
            }
            catch (Exception)
            {
                //user abort selection or other
                return null;
            }
        }

        #endregion

        #region FailureProcessor HelperClass for Spaces

        public class SpaceExistWarner : IFailuresPreprocessor
        {
            public FailureProcessingResult PreprocessFailures(FailuresAccessor a)
            {
                var failures = a.GetFailureMessages();
                foreach (var f in failures)
                {
                    var id = f.GetFailureDefinitionId();
                    if (BuiltInFailures.GeneralFailures.DuplicateValue == id) a.DeleteWarning(f);

                    if (BuiltInFailures.RoomFailures.RoomsInSameRegionSpaces == id)
                    {
                        WarnElements = f.GetFailingElementIds();
                        a.DeleteWarning(f);
                        return FailureProcessingResult.ProceedWithRollBack;
                    }
                }

                return FailureProcessingResult.Continue;
            }
        }

        #endregion

        #region Helper Method for Levels by Elevation

        private Level GetLevelByElevation(Document _doc, double _elevation)
        {
            return new FilteredElementCollector(_doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .Where(l => l.Elevation.Equals(_elevation))
                .FirstOrDefault();
        }

        #endregion

        private List<SpacesData> spacesDataList;
        private List<RoomsData> roomsDataList;
        private Document DOC;
        private Document linkedDOC;
        private List<LevelsData> levelsDataCreation;
        private Transform tGlobal;
        private Stopwatch sTimer = new Stopwatch();

        public void CreateSpaces()
        {
            //Check if Document is opened in UI and Document is Project
            sTimer.Reset();
            if (null != this.Application.ActiveUIDocument &&
                !this.Application.ActiveUIDocument.Document.IsFamilyDocument)
            {
                DOC = this.Application.ActiveUIDocument.Document;
                //Start taskdialog for select link
                if (TdSelectARLink() == 0) return;

                var arLink = GetARLinkReference();
                if (null != arLink)
                {
                    //Get value for Room bounding in RevitLinkType
                    var selInstance = DOC.GetElement(arLink.ElementId) as RevitLinkInstance;
                    var lnkType = DOC.GetElement(selInstance.GetTypeId()) as RevitLinkType;
                    var boundingWalls =
                        Convert.ToBoolean(lnkType.get_Parameter(BuiltInParameter.WALL_ATTR_ROOM_BOUNDING).AsInteger());
                    //Get coordination transformation for link
                    tGlobal = selInstance.GetTotalTransform();
                    //Get Document from RvtLinkInstance
                    linkedDOC = selInstance.GetLinkDocument();
                    //Check for valid Document and Room bounding value checked
                    if (null != linkedDOC && !boundingWalls)
                    {
                        TaskDialog.Show("Ошибка",
                            "Нет загруженной связи АР или в связанном файле не включен поиск границ помещения!\nДля размещения пространств необходимо включить этот параметр");
                        return;
                    }

                    //Mainline code
                    //Get placed Spaces and Levels information
                    spacesDataList = GetSpaces(DOC);
                    roomsDataList = GetRooms(linkedDOC);
                    //Check if Spaces placed
                    if (roomsDataList.Count > 0)
                    {
                        if (spacesDataList.Count == 0)
                            switch (TdFirstTime())
                            {
                                case 0:
                                    return;
                                case 1:
                                    AnaliseAR();
                                    break;
                                default:
                                    return;
                            }
                        else
                            AnaliseAR();
                    }
                    else
                    {
                        var tDialog = new TaskDialog("No Rooms in link");
                        tDialog.Title = "Нет помещений";
                        tDialog.MainInstruction = "В выбранном экземпляре связи нет помещений.";
                        tDialog.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                        tDialog.TitleAutoPrefix = false;
                        tDialog.CommonButtons = TaskDialogCommonButtons.Close;
                        tDialog.Show();
                    }
                }
            }
        }

        private void AnaliseAR()
        {
            levelsDataCreation = MatchLevels(roomsDataList.Select(r => r.RoomsLevel).ToList());
            if (levelsDataCreation.Count > 0)
                switch (TdLevelsCreate())
                {
                    case 0:
                        return;
                    case 1:
                        sTimer.Start();
                        CreateLevels(levelsDataCreation);
                        sTimer.Stop();
                        break;
                    default:

                        break;
                }

            switch (TdSpacesPlace())
            {
                case 0:
                    return;
                case 1:
                    sTimer.Start();
                    CreateSpByRooms(true);
                    break;
                case 2:
                    sTimer.Start();
                    CreateSpByRooms(false);
                    break;
                default:

                    break;
            }
        }

        #region TaskDialogs

        private int TdSpacesPlace()
        {
            var td = new TaskDialog("Spaces place Type");
            td.Id = "ID_TaskDialog_Spaces_Type_Place";
            td.MainIcon = TaskDialogIcon.TaskDialogIconInformation;
            td.Title = "Создание/обновление пространств";
            td.TitleAutoPrefix = false;
            td.AllowCancellation = true;
            // Message related stuffs
            td.MainInstruction = "Настройка задания верхнего предела и смещения для размещения пространств";
            td.MainContent = "Выберите способ создания/обновления пространств";
            td.ExpandedContent =
                "При выборе варианта по помещениям - пространства создаются с копированием всех настроект для привязки верхнего уровня и смещения из помещений. Так же будут дополнительно скопированы уровни для верхнего предела, если таковые отсутсвуют.\n" +
                "При выборе варианта по уровням - верхним пределом для пространств будет следующий уровень с помещениями и значение смещения 0 мм. Если такового не существует (последний уровень с помещениями) используется смещение 3500 мм.";
            // Command link stuffs
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "По помещениям");
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "По уровням");
            // Dialog showup stuffs
            var tdRes = td.Show();
            switch (tdRes)
            {
                case TaskDialogResult.Cancel:
                    return 0;
                case TaskDialogResult.CommandLink1:
                    return 1;
                case TaskDialogResult.CommandLink2:
                    return 2;
                default:
                    throw new Exception("Invalid value for TaskDialogResult");
            }
        }

        private int TdLevelsCreate()
        {
            var td = new TaskDialog("Levels Create for Spaces");

            td.Id = "ID_TaskDialog_Create_Levels";
            td.MainIcon = TaskDialogIcon.TaskDialogIconInformation;
            td.Title = "Создание недостающих уровней";
            td.TitleAutoPrefix = false;
            td.AllowCancellation = true;
            // Message related stuffs
            td.MainInstruction = "В текущем проекте не хватет уровней для создания инженерных пространств";
            td.MainContent = "Создать уровни в текущем проекте";
            td.ExpandedContent =
                "В выбранном файле архитектуры имеются помещения, размещенные на уровнях, отсутсвующих в текущем проекте.\nНеобходимо создать уровни в текущем проекте для автоматического размещения инженерных пространств\n" +
                "Будут созданы уровни с префиксом АР_имя уровня для всех помещений";
            // Command link stuffs
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Продолжить");
            // Dialog showup stuffs
            var tdRes = td.Show();
            switch (tdRes)
            {
                case TaskDialogResult.Cancel:
                    return 0;
                case TaskDialogResult.CommandLink1:
                    return 1;
                //break;
                default:
                    throw new Exception("Invalid value for TaskDialogResult");
            }
        }

        private int TdFirstTime()
        {
            var td = new TaskDialog("First time Spaces placing");
            td.Id = "ID_TaskDialog_Place_Spaces_Type";
            td.MainIcon = TaskDialogIcon.TaskDialogIconInformation;
            td.Title = "Размещение инженерных пространств";
            td.TitleAutoPrefix = false;
            td.AllowCancellation = true;
            // Message related stuffs
            td.MainInstruction = "Втекущем проекте нет размещенных инженерных пространств";
            td.MainContent = "Проанализировать связанный файл на наличие помещений";
            // Command link stuffs
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Продолжить");
            // Dialog showup stuffs
            var tdRes = td.Show();
            switch (tdRes)
            {
                case TaskDialogResult.Cancel:
                    return 0;
                case TaskDialogResult.CommandLink1:
                    return 1;
                //break;
                default:
                    throw new Exception("Invalid value for TaskDialogResult");
            }
        }

        private int TdSelectARLink()
        {
            var td = new TaskDialog("Select Link File");

            td.Id = "ID_TaskDialog_Select_AR";
            td.MainIcon = TaskDialogIcon.TaskDialogIconInformation;
            td.Title = "Выбор экземпляра размещенной связи";
            td.TitleAutoPrefix = false;
            td.AllowCancellation = true;
            // Message related stuffs
            td.MainInstruction = "Выберите экземпляр размещенной связи для поиска помещений";
            // Command link stuffs
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Выбрать");
            // Dialog showup stuffs
            var tdRes = td.Show();
            switch (tdRes)
            {
                case TaskDialogResult.Cancel:
                    return 0;
                case TaskDialogResult.CommandLink1:
                    return 1;
                default:
                    throw new Exception("Invalid value for TaskDialogResult");
            }
        }

        #endregion

        private void CreateSpByRooms(bool RoomLimits)
        {
            var levels = 0;
            var sCreated = 0;
            var sUpdated = 0;
            double defLimitOffset = 3500;
            using (var crTrans = new TransactionGroup(DOC, "Создание пространств"))
            {
                crTrans.Start();
                foreach (var roomsData in roomsDataList)
                {
                    var lLevel = roomsData.RoomsLevel;
                    var localLevel = GetLevelByElevation(DOC, lLevel.Elevation);

                    if (null != localLevel)
                    {
                        levels++;
                        foreach (var lRoom in roomsData.RoomsList)
                        {
                            var RoomName = lRoom.get_Parameter(BuiltInParameter.ROOM_NAME).AsString();
                            var RoomNumber = lRoom.get_Parameter(BuiltInParameter.ROOM_NUMBER).AsString();
                            if (lRoom.Level.Elevation.Equals(localLevel.Elevation))
                                using (var tr = new Transaction(DOC, "Create space"))
                                {
                                    tr.Start();
                                    var failOpt = tr.GetFailureHandlingOptions();
                                    failOpt.SetFailuresPreprocessor(new SpaceExistWarner());
                                    tr.SetFailureHandlingOptions(failOpt);
                                    var lp = lRoom.Location as LocationPoint;
                                    var rCoord = tGlobal.OfPoint(lp.Point);
                                    Space sp = null;
                                    var spLocPoint = new UV(rCoord.X, rCoord.Y);
                                    sp = DOC.Create.NewSpace(localLevel, spLocPoint);
                                    var trStat = tr.Commit(failOpt);
                                    //Space not exists, change name and number for new space
                                    if (trStat == TransactionStatus.Committed)
                                    {
                                        tr.Start();
                                        sp.get_Parameter(BuiltInParameter.ROOM_NAME).Set(RoomName);
                                        sp.get_Parameter(BuiltInParameter.ROOM_NUMBER).Set(RoomNumber);
                                        //TryToRenameSpace(tr,sp,RoomName,RoomNumber);
                                        if (RoomLimits)
                                        {
                                            var upperLevel = GetLevelByElevation(DOC, lRoom.UpperLimit.Elevation);
                                            if (null == upperLevel)
                                            {
                                                upperLevel = Level.Create(DOC, lRoom.UpperLimit.Elevation);
                                                upperLevel.Name = "АР_" + lRoom.UpperLimit.Name;
                                            }
                                        }
                                        else
                                        {
                                            if (roomsData.RoomsLevel == roomsData.UpperRoomLevel)
                                            {
                                                var upperLevel = localLevel;
                                                //sp.get_Parameter(BuiltInParameter.ROOM_UPPER_LEVEL).Set(localLevel.LevelId);
                                                sp.UpperLimit = GetLevelByElevation(DOC, localLevel.Elevation);
                                                sp.get_Parameter(BuiltInParameter.ROOM_UPPER_OFFSET)
                                                    .Set(UnitUtils.ConvertToInternalUnits(defLimitOffset,
                                                        DisplayUnitType.DUT_MILLIMETERS));
                                            }
                                            else
                                            {
                                                var upperLevel = roomsData.UpperRoomLevel;
                                                sp.UpperLimit = GetLevelByElevation(DOC, upperLevel.Elevation);
                                                sp.get_Parameter(BuiltInParameter.ROOM_UPPER_OFFSET).Set(0);
                                            }
                                        }

                                        tr.Commit(failOpt);
                                        sCreated++;
                                    }
                                    //If Space placed in same area. Transaction creating space Rolledback
                                    else
                                    {
                                        foreach (var eId in WarnElements)
                                            if (null != DOC.GetElement(eId) && DOC.GetElement(eId) is Space)
                                            {
                                                var wSpace = DOC.GetElement(eId) as Space;
                                                //bool updated = false;
                                                var SpaceName = wSpace.get_Parameter(BuiltInParameter.ROOM_NAME)
                                                    .AsString();
                                                var SpaceNumber = wSpace.get_Parameter(BuiltInParameter.ROOM_NUMBER)
                                                    .AsString();
                                                var sLp = wSpace.Location as LocationPoint;

                                                tr.Start();
                                                sLp.Point = rCoord;
                                                wSpace.get_Parameter(BuiltInParameter.ROOM_NAME).Set(RoomName);
                                                wSpace.get_Parameter(BuiltInParameter.ROOM_NUMBER).Set(RoomNumber);
                                                if (RoomLimits)
                                                {
                                                    var upperLevel =
                                                        GetLevelByElevation(DOC, lRoom.UpperLimit.Elevation);
                                                    if (null == upperLevel)
                                                    {
                                                        upperLevel = Level.Create(DOC, lRoom.UpperLimit.Elevation);
                                                        upperLevel.Name = "АР_" + lRoom.UpperLimit.Name;
                                                    }

                                                    wSpace.UpperLimit = upperLevel;
                                                    wSpace.get_Parameter(BuiltInParameter.ROOM_UPPER_OFFSET)
                                                        .Set(lRoom.LimitOffset);
                                                }
                                                else
                                                {
                                                    if (roomsData.RoomsLevel == roomsData.UpperRoomLevel)
                                                    {
                                                        var upperLevel = localLevel;
                                                        wSpace.UpperLimit = upperLevel;
                                                        wSpace.get_Parameter(BuiltInParameter.ROOM_UPPER_OFFSET)
                                                            .Set(UnitUtils.ConvertToInternalUnits(defLimitOffset,
                                                                DisplayUnitType.DUT_MILLIMETERS));
                                                    }
                                                    else
                                                    {
                                                        var upperLevel = roomsData.UpperRoomLevel;
                                                        wSpace.UpperLimit =
                                                            GetLevelByElevation(DOC, upperLevel.Elevation);
                                                        wSpace.get_Parameter(BuiltInParameter.ROOM_UPPER_OFFSET).Set(0);
                                                    }
                                                }

                                                tr.Commit();
                                                sUpdated++;
                                            }
                                    }
                                }
                        }
                    }
                }

                crTrans.Assimilate();
            }

            sTimer.Stop();
            var resTimer = sTimer.Elapsed;
            var tDialog = new TaskDialog("Rsult data");
            tDialog.Title = "Отчёт";
            tDialog.MainInstruction = string.Format("Обновлено {0}\nСоздано {1}", sUpdated, sCreated);
            tDialog.MainIcon = TaskDialogIcon.TaskDialogIconShield;
            tDialog.TitleAutoPrefix = false;
            tDialog.FooterText = string.Format("Общее время {0:D2}:{1:D2}:{2:D3}", resTimer.Minutes, resTimer.Seconds,
                resTimer.Milliseconds);
            tDialog.CommonButtons = TaskDialogCommonButtons.Close;
            tDialog.Show();
        }

        #endregion

        #region AutoNumerate

        public void AutoNumeratePos()
        {
            Document doc = this.Document;
            var Sub = false;
            if (doc.ActiveView.ViewType == ViewType.Schedule)
            {
                var td = new TaskDialog("Autonumerate");

                td.Id = "ID_TaskDialog_Autonumerates";
                td.MainIcon = TaskDialogIcon.TaskDialogIconInformation;
                td.Title = "Автонумерация позиции";
                td.TitleAutoPrefix = false;
                td.AllowCancellation = true;
                td.MainInstruction =
                    "Для автонумерации позиции могут использоваться номер строки или индекс вложенных семейств";
                td.MainContent = "Выберите способ автонумерации:";
                td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "По строке элемента в спецификации");
                td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "По вложенным семействам");
                var tdRes = td.Show();
                if (tdRes == TaskDialogResult.CommandLink1) Sub = false;

                if (tdRes == TaskDialogResult.CommandLink2) Sub = true;

                var locVS = ActiveView as ViewSchedule;
                var tableData = locVS.GetTableData();
                var tableSectionData = tableData.GetSectionData(SectionType.Body);
                if (tableSectionData.NumberOfRows > 0)
                {
                    var startIndex = 1; //Стартовый значение для номера

                    using (var tGroup =
                        new TransactionGroup(doc, "Автонумерация спецификации: " + locVS.Name))
                    {
                        tGroup.Start();
                        for (var rInd = 0; rInd < tableSectionData.NumberOfRows; rInd++)
                        {
                            var elementsOnRow = GetElementsOnRow(doc, locVS, rInd);
                            if (null != elementsOnRow)
                            {
                                if (Sub)
                                    startIndex = SetNum(doc, startIndex, elementsOnRow, true);
                                else
                                    SetNum(doc, startIndex++, elementsOnRow, false);
                            }
                        }

                        tGroup.Assimilate();
                    }
                }
            }
            else
            {
                TaskDialog.Show("Предупреждение", "Для автонумерации требуется открыть спецификацию!");
            }
        }

        private int SetNum(Document doc, int num, List<Element> elements, bool sub)
        {
            var hostFams = false;
            var system = false;
            var upped = false;
            var pos = num;
            using (var tr = new Transaction(doc, "Задание номера позиции элементам"))
            {
                tr.Start();
                if (!sub)
                {
                    foreach (var curElement in elements)
                    {
                        var curFi = doc.GetElement(curElement.Id) as FamilyInstance;
                        if (null != curFi)
                        {
                            if (curFi.GetSubComponentIds().Count > 0 && null == curFi.SuperComponent)
                            {
                                curElement.get_Parameter(ADSK_POS)
                                    .Set(num.ToString()); // Заполнение параметра ADSK_Позиция
                                hostFams = true;
                            }
                            else if (!hostFams)
                            {
                                curElement.get_Parameter(ADSK_POS)
                                    .Set(num.ToString()); // Заполнение параметра ADSK_Позиция
                            }
                        }
                        else
                        {
                            curElement.get_Parameter(ADSK_POS).Set(num.ToString()); // Заполнение параметра ADSK_Позиция
                        }
                    }
                }
                else
                {
                    foreach (var curElement in elements)
                    {
                        var curFi = doc.GetElement(curElement.Id) as FamilyInstance;
                        if (null != curFi)
                        {
                            if (curFi.GetSubComponentIds().Count > 0 && null == curFi.SuperComponent)
                            {
                                curElement.get_Parameter(ADSK_POS)
                                    .Set(pos.ToString()); // Заполнение параметра ADSK_Позиция
                                var subNum = 1;
                                foreach (var subelemId in curFi.GetSubComponentIds())
                                {
                                    Element subelem = doc.GetElement(subelemId) as FamilyInstance;
                                    subelem.get_Parameter(ADSK_POS)
                                        .Set(pos.ToString() + "." +
                                             subNum.ToString()); // Заполнение параметра ADSK_Позиция
                                    subNum++;
                                }

                                hostFams = true;
                            }
                            else if (null == curFi.SuperComponent)
                            {
                                curElement.get_Parameter(ADSK_POS)
                                    .Set(num.ToString()); // Заполнение параметра ADSK_Позиция
                                var Super = elements.Cast<FamilyInstance>().Where(e => e.GetSubComponentIds().Count > 0)
                                    .Count();
                                if (Super == 0 && !upped)
                                {
                                    pos++;
                                    upped = true;
                                }
                            }
                        }
                        else
                        {
                            curElement.get_Parameter(ADSK_POS).Set(num.ToString()); // Заполнение параметра ADSK_Позиция
                            system = true;
                            //pos++;
                        }
                    }

                    if (hostFams | system) pos++;
                }

                tr.Commit();
            }

            return pos;
        }

        #endregion

        #region ADSK Check

        public void CheckADSK()
        {
            Document doc = this.Document;
            var log =
                new StringBuilder(
                    "Имя семейства;ADSK_Наименование;ADSK_Марка;ADSK_Код изделия;ADSK_Завод-изготовитель;ADSK_Единица измерения;ADSK_Количество;ADSK_Масса\n");
            //TaskDialog.Show("Test",CheckOpenedFamilies(doc)?"No families":"Close first!");
            if (CheckOpenedFamilies(doc))
            {
                var fPath = SelectFolder();
                var logFile = Path.Combine(fPath, "Log_ADSK_Checker.csv");
                if (fPath != "")
                {
                    var l_files = new List<string>();
                    var l_files_bck = new List<string>();
                    var files = Directory.GetFiles(fPath, "*.rfa", SearchOption.AllDirectories);
                    l_files.AddRange(files);
                    var files_bck = Directory.GetFiles(fPath, "*.????.rfa", SearchOption.AllDirectories);
                    l_files_bck.AddRange(files_bck);
                    if (files.Length > 0)
                        foreach (var fl in l_files)
                            if (!l_files_bck.Contains(fl))
                            {
                                var mPath = ModelPathUtils.ConvertUserVisiblePathToModelPath(fl);
                                var oOps = new OpenOptions();
                                oOps.Audit = true;
                                Document famDoc = null;
                                try
                                {
                                    famDoc = doc.Application.OpenDocumentFile(mPath, oOps);
                                }
                                catch (Exception)
                                {
                                    throw;
                                }

                                var fam = famDoc.OwnerFamily;
                                if (fam.FamilyCategory.CategoryType == CategoryType.Model)
                                {
                                    var rfaName = Path.GetFileName(fl);
                                    log.AppendLine(rfaName + CheckFamily(famDoc));
                                }

                                famDoc.Close(false);
                            }

                    var text = log.ToString().Replace(';', '\t');
                    using (var swriter = new StreamWriter(logFile))
                    {
                        swriter.Write(text, Encoding.ASCII);
                    }

                    var td = new TaskDialog("Check_fam");

                    td.Id = "ID_TaskDialog_Checked_Fams";
                    td.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                    td.Title = "Проверка файлов семейств";
                    td.TitleAutoPrefix = false;
                    td.AllowCancellation = true;
                    td.MainInstruction = "Семейства проверены, файл отчет находится в корневой папке поиска";
                    td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "ОК");
                    td.Show();
                }
            }
            else
            {
                var td = new TaskDialog("Warn_fam");

                td.Id = "ID_TaskDialog_Warn_Fams";
                td.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                td.Title = "Проверка файлов семейств";
                td.TitleAutoPrefix = false;
                td.AllowCancellation = true;
                td.MainInstruction =
                    "Перед пакетной проверкой семейств необходимо закрыть все текущие открытые семейства!";
                td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "ОК");
                td.Show();
            }
        }

        private string SelectFolder()
        {
            var fBrowser = new WForm.FolderBrowserDialog();
            fBrowser.RootFolder = Environment.SpecialFolder.MyComputer;
            var resultDG = fBrowser.ShowDialog();
            if (resultDG == WForm.DialogResult.OK)
                return fBrowser.SelectedPath;
            else
                return "";
        }

        private string CheckFamily(Document _doc)
        {
            var famManager = _doc.FamilyManager;
            var res = "";
            IList<FamilyParameter> listFamParams = famManager.GetParameters().Where(fp => fp.IsShared).ToList();
            IList<Guid> listGuids = listFamParams.Select(p => p.GUID).ToList();
            if (listGuids.Count > 0)
            {
                if (listGuids.Contains(ADSK_NAIMEN))
                    res += ";+;";
                else
                    res += "-;";

                if (listGuids.Contains(ADSK_TIP))
                    res += "+;";
                else
                    res += "-;";

                if (listGuids.Contains(ADSK_CODE))
                    res += "+;";
                else
                    res += "-;";

                if (listGuids.Contains(ADSK_ZAVOD))
                    res += "+;";
                else
                    res += "-;";

                if (listGuids.Contains(ADSK_EDIZM))
                    res += "+;";
                else
                    res += "-;";

                if (listGuids.Contains(ADSK_COL))
                    res += "+;";
                else
                    res += "-;";

                if (listGuids.Contains(ADSK_MASSKG))
                    res += "+";
                else
                    res += "-";
            }

            return res;
        }

        private bool CheckOpenedFamilies(Document _doc)
        {
            var docSet = _doc.Application.Documents;
            foreach (Document curDoc in docSet)
                if (curDoc.IsFamilyDocument)
                    return false;

            return true;
        }

        #endregion

        #region Revit Macros generated code

        private void InternalStartup()
        {
            this.Startup += new EventHandler(Module_Startup);
            this.Shutdown += new EventHandler(Module_Shutdown);
        }

        #endregion
    }
}
