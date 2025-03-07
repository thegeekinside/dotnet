﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    public static class WellKnownCommandNames
    {
        public const string Build_BuildSolution = "Build.BuildSolution";

        public const string Edit_ListMembers = "Edit.ListMembers";
        public const string Edit_ToggleCompletionMode = "Edit.ToggleCompletionMode";
        public const string Edit_Undo = "Edit.Undo";
        public const string Edit_Redo = "Edit.Redo";
        public const string Edit_SelectionCancel = "Edit.SelectionCancel";
        public const string Edit_LineStart = "Edit.LineStart";
        public const string Edit_LineEnd = "Edit.LineEnd";
        public const string Edit_LineStartExtend = "Edit.LineStartExtend";
        public const string Edit_LineEndExtend = "Edit.LineEndExtend";
        public const string Edit_SelectAll = "Edit.SelectAll";
        public const string Edit_Copy = "Edit.Copy";
        public const string Edit_Cut = "Edit.Cut";
        public const string Edit_Paste = "Edit.Paste";
        public const string Edit_Delete = "Edit.Delete";
        public const string Edit_LineUp = "Edit.LineUp";
        public const string Edit_LineDown = "Edit.LineDown";

        public const string File_SaveAll = "File.SaveAll";

        public const string InteractiveConsole_ClearScreen = "InteractiveConsole.ClearScreen";
        public const string InteractiveConsole_ExecuteInInteractive = "InteractiveConsole.ExecuteInInteractive";

        public const string ProjectAndSolutionContextMenus_Project_ResetCSharpInteractiveFromProject
            = "ProjectandSolutionContextMenus.Project.ResetC#InteractiveFromProject";

        public const string Refactor_Rename = "Refactor.Rename";
        public const string Refactor_ExtractMethod = "Refactor.ExtractMethod";
        public const string Refactor_ExtractInterface = "Refactor.ExtractInterface";
        public const string Refactor_EncapsulateField = "Refactor.EncapsulateField";
        public const string Refactor_RemoveParameters = "Refactor.RemoveParameters";
        public const string Refactor_ReorderParameters = "Refactor.ReorderParameters";

        public const string Test_IntegrationTestService_Start = "Test.IntegrationTestService.Start";
        public const string Test_IntegrationTestService_Stop = "Test.IntegrationTestService.Stop";
    }
}
