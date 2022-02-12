using MDSDKBase;
using MDSDKDerived;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace MDSDK
{
    /// <summary>
    /// An object model representing the Win32 and COM API reference content.
    /// </summary>
    internal class ApiRefModelWin32
    {
        private List<FreeFunctionWin32> freeFunctionWin32s = new List<FreeFunctionWin32>();
        private List<EnumerationWin32> enumerationWin32s = new List<EnumerationWin32>();
        private List<StructureWin32> structureWin32s = new List<StructureWin32>();
        private List<InterfaceCOM> interfaceCOMs = new List<InterfaceCOM>();
        private List<MethodCOM> methodCOMs = new List<MethodCOM>();
        private List<CallbackFunctionWin32> callbackFunctionWin32s = new List<CallbackFunctionWin32>();
        private List<ClassWin32> classWin32s = new List<ClassWin32>();
        private List<IoctlWin32> ioctlWin32s = new List<IoctlWin32>();

        public void AddFreeFunctionWin32(string name, string headerName, string topicUrl, string moduleName = null, List<string> libraryNames = null)
        {
            this.freeFunctionWin32s.Add(new FreeFunctionWin32(name, headerName, topicUrl, moduleName, libraryNames));
        }

        public void AddEnumerationWin32(string name, string headerName, string topicUrl, string moduleName = null, List<string> libraryNames = null)
        {
            this.enumerationWin32s.Add(new EnumerationWin32(name, headerName, topicUrl, moduleName, libraryNames));
        }

        public void AddStructureWin32(string name, string headerName, string topicUrl, string moduleName = null, List<string> libraryNames = null)
        {
            this.structureWin32s.Add(new StructureWin32(name, headerName, topicUrl, moduleName, libraryNames));
        }

        public InterfaceCOM AddInterfaceCOM(string name, string headerName, string topicUrl, string moduleName = null, List<string> libraryNames = null)
        {
            var interfaceCOM = new InterfaceCOM(name, headerName, topicUrl, moduleName, libraryNames);
            this.interfaceCOMs.Add(interfaceCOM);
            return interfaceCOM;
        }

        public void AddMethodCOM(string name, string headerName, string topicUrl, InterfaceCOM theInterface, string moduleName = null, List<string> libraryNames = null)
        {
            this.methodCOMs.Add(new MethodCOM(name, headerName, topicUrl, theInterface, moduleName, libraryNames));
        }

        public void AddCallbackFunctionWin32(string name, string headerName, string topicUrl, string moduleName = null, List<string> libraryNames = null)
        {
            this.callbackFunctionWin32s.Add(new CallbackFunctionWin32(name, headerName, topicUrl, moduleName, libraryNames));
        }

        public void AddClassWin32(string name, string headerName, string topicUrl, string moduleName = null, List<string> libraryNames = null)
        {
            this.classWin32s.Add(new ClassWin32(name, headerName, topicUrl, moduleName, libraryNames));
        }

        public void AddIoctlWin32(string name, string headerName, string topicUrl, string moduleName = null, List<string> libraryNames = null)
        {
            this.ioctlWin32s.Add(new IoctlWin32(name, headerName, topicUrl, moduleName, libraryNames));
        }

        //// Note: the key here is the ToLower version but the FreeFunctionWin32 contains the original case from the topic's title.
        //public Dictionary<string, FunctionWin32InDocs> FunctionWin32InDocses = new Dictionary<string, FunctionWin32InDocs>();

        //public void EnsureFunction(Editor eachTopicEditor, string projectName, string id, string name, FileInfo fileInfo, ref bool functionAlreadyExists, ref FunctionWin32InDocs functionWin32)
        //{
        //    if (this.FunctionWin32InDocses.ContainsKey(name.ToLower()))
        //    {
        //        functionAlreadyExists = true;
        //        functionWin32 = this.FunctionWin32InDocses[name.ToLower()];
        //    }
        //    else
        //    {
        //        functionAlreadyExists = false;
        //        this.FunctionWin32InDocses[name.ToLower()] = new FunctionWin32InDocs(projectName, id, name, fileInfo, eachTopicEditor.GetLibraryFilenames());
        //    }
        //}

        //public bool HasFunctionWin32s
        //{
        //    get
        //    {
        //        return (FunctionWin32InDocses.Count != 0);
        //    }
        //}

        //public bool GetFunctionWin32ByName(string name, ref FunctionWin32InDocs foundFunctionWin32)
        //{
        //    if (this.FunctionWin32InDocses.ContainsKey(name.ToLower()))
        //    {
        //        foundFunctionWin32 = this.FunctionWin32InDocses[name.ToLower()];
        //        return true;
        //    }
        //    return false;
        //}
    }

    internal abstract class Win32OrCOMAPI
    {
        public string HeaderName { get; set; }
        public List<string> LibraryNames { get; set; }
        public string ModuleName { get; set; }
        public string Name { get; set; }
        public string TopicUrl { get; set; }

        public Win32OrCOMAPI(string name, string headerName, string topicUrl, string moduleName, List<string> libraryNames)
        {
            this.HeaderName = name;
            this.LibraryNames = libraryNames;
            this.ModuleName = moduleName;
            this.Name = headerName;
            this.TopicUrl = topicUrl;
        }

        public string MarkdownLink()
        {
            return $"[{this.Name}]({this.TopicUrl})";
        }
    }

    /// <summary>
    /// A class representing a Win32 free function.
    /// </summary>
    internal class FreeFunctionWin32 : Win32OrCOMAPI
    {
        public FreeFunctionWin32(string name, string headerName, string topicUrl, string moduleName = null, List<string> libraryNames = null) :
        base(name, headerName, topicUrl, moduleName, libraryNames)
        { }
    }

    /// <summary>
    /// A class representing a Win32 enumeration.
    /// </summary>
    internal class EnumerationWin32 : Win32OrCOMAPI
    {
        public EnumerationWin32(string name, string headerName, string topicUrl, string moduleName = null, List<string> libraryNames = null) :
        base(name, headerName, topicUrl, moduleName, libraryNames)
        { }
    }

    /// <summary>
    /// A class representing a Win32 structure.
    /// </summary>
    internal class StructureWin32 : Win32OrCOMAPI
    {
        public StructureWin32(string name, string headerName, string topicUrl, string moduleName = null, List<string> libraryNames = null) :
        base(name, headerName, topicUrl, moduleName, libraryNames)
        { }
    }

    /// <summary>
    /// A class representing a COM interface.
    /// </summary>
    internal class InterfaceCOM : Win32OrCOMAPI
    {
        public List<string> MethodNames { get; set; }

        public InterfaceCOM(string name, string headerName, string topicUrl, string moduleName = null, List<string> libraryNames = null) :
        base(name, headerName, topicUrl, moduleName, libraryNames)
        {
            this.MethodNames = new List<string>();
        }
    }

    /// <summary>
    /// A class representing a COM method.
    /// </summary>
    internal class MethodCOM : Win32OrCOMAPI
    {
        public InterfaceCOM Interface { get; set; }

        public MethodCOM(string name, string headerName, string topicUrl, InterfaceCOM theInterface, string moduleName = null, List<string> libraryNames = null) :
        base(name, headerName, topicUrl, moduleName, libraryNames)
        {
            this.Interface = theInterface;
        }
    }

    /// <summary>
    /// A class representing a Win32 callback function.
    /// </summary>
    internal class CallbackFunctionWin32 : Win32OrCOMAPI
    {
        public CallbackFunctionWin32(string name, string headerName, string topicUrl, string moduleName = null, List<string> libraryNames = null) :
        base(name, headerName, topicUrl, moduleName, libraryNames)
        { }
    }

    /// <summary>
    /// A class representing a Win32 class.
    /// </summary>
    internal class ClassWin32 : Win32OrCOMAPI
    {
        public List<string> MemberFunctionNames { get; set; }

        public ClassWin32(string name, string headerName, string topicUrl, string moduleName = null, List<string> libraryNames = null) :
        base(name, headerName, topicUrl, moduleName, libraryNames)
        {
            this.MemberFunctionNames = new List<string>();
        }
    }

    /// <summary>
    /// A class representing a Win32 IOCTL.
    /// </summary>
    internal class IoctlWin32 : Win32OrCOMAPI
    {
        public IoctlWin32(string name, string headerName, string topicUrl, string moduleName = null, List<string> libraryNames = null) :
        base(name, headerName, topicUrl, moduleName, libraryNames)
        { }
    }

    /// <summary>
    /// Utility class used for sorting FunctionWin32s.
    /// </summary>
    internal class FunctionWin32Comparer : Comparer<FreeFunctionWin32>
    {
        public override int Compare(FreeFunctionWin32 lhs, FreeFunctionWin32 rhs)
        {
            return lhs.Name.CompareTo(rhs.Name);
        }
    }

    ///// <summary>
    ///// Utility class used for sorting FunctionWin32GroupedByInitialChars.
    ///// </summary>
    //internal class FunctionWin32GroupedByInitialCharComparer : Comparer<FunctionWin32GroupedByInitialChar>
    //{
    //    public override int Compare(FunctionWin32GroupedByInitialChar lhs, FunctionWin32GroupedByInitialChar rhs)
    //    {
    //        return lhs.Name.CompareTo(rhs.Name);
    //    }
    //}

    ///// <summary>
    ///// Utility class used for sorting InitialCharGroups.
    ///// </summary>
    //internal class InitialCharGroupComparer : Comparer<InitialCharGroup>
    //{
    //    public override int Compare(InitialCharGroup lhs, InitialCharGroup rhs)
    //    {
    //        return lhs.Name.CompareTo(rhs.Name);
    //    }
    //}

    ///// <summary>
    ///// A class representing a Win32 function in the docs.
    ///// </summary>
    //internal class FunctionWin32InDocs : FreeFunctionWin32
    //{
    //    public FunctionWin32InDocs(string projectName, string id, string name, FileInfo fileInfo, List<string> libraryNames)
    //        : base(name, null, libraryNames)
    //    {
    //        this.ProjectName = projectName;
    //        this.Id = id;
    //        this.Name = name;
    //        this.FileInfo = fileInfo;
    //    }

    //    public FunctionWin32InDocs(string projectName, string id, string name, FileInfo fileInfo)
    //        : this(projectName, id, name, fileInfo, null)
    //    {
    //    }

    //    public string ProjectName = string.Empty;
    //    public string Id = string.Empty;
    //    public FileInfo FileInfo;
    //}

    /// <summary>
    /// A class representing an api grouped by some key.
    /// </summary>
    internal class FunctionWin32Grouped : FreeFunctionWin32
    {
        public string SdkVersionIntroducedIn { get; set; }
        public string SdkVersionRemovedIn { get; set; }
        public string FunctionWin32Id { get; set; }
        public Module ModuleMovedTo { get; set; }

        public FunctionWin32Grouped(string moduleName, string name, string sdkVersionIntroducedIn, string functionWin32Id)
            : base(name, moduleName, null)
        {
            this.SdkVersionIntroducedIn = sdkVersionIntroducedIn;
            this.FunctionWin32Id = functionWin32Id;
        }
    }

    internal class FunctionWin32GroupedByModule : FunctionWin32Grouped
    {
        public string Requirements
        {
            get
            {
                string requirements = "Introduced in Windows " + this.SdkVersionIntroducedIn;
                if (this.SdkVersionRemovedIn != null)
                {
                    if (this.ModuleMovedTo != null)
                    {
                        requirements += ". Moved to " + this.ModuleMovedTo.Name + " in Windows " + this.SdkVersionRemovedIn;
                    }
                    else
                    {
                        requirements += ". Removed in Windows " + this.SdkVersionRemovedIn;
                    }
                }
                return requirements;
            }
        }

        public FunctionWin32GroupedByModule(string moduleName, string name, string sdkVersionIntroducedIn, string functionWin32Id)
            : base(moduleName, name, sdkVersionIntroducedIn, functionWin32Id)
        {
        }
    }

    //internal class FunctionWin32GroupedByInitialChar : FunctionWin32Grouped
    //{
    //    public string Module
    //    {
    //        get
    //        {
    //            string requirements = "Introduced into " + this.ModuleName + " in Windows " + this.SdkVersionIntroducedIn;
    //            if (this.SdkVersionRemovedIn != null)
    //            {
    //                if (this.ModuleMovedTo != null)
    //                {
    //                    requirements += ". Moved to " + this.ModuleMovedTo.Name + " in Windows " + this.SdkVersionRemovedIn;
    //                }
    //                else
    //                {
    //                    requirements += ". Removed in Windows " + this.SdkVersionRemovedIn;
    //                }
    //            }
    //            return requirements;
    //        }
    //    }

    //    public FunctionWin32GroupedByInitialChar(string moduleName, string name, string sdkVersionIntroducedIn, string functionWin32Id, string sdkVersionRemovedIn, Module moduleMovedTo)
    //        : base(moduleName, name, sdkVersionIntroducedIn, functionWin32Id)
    //    {
    //        this.SdkVersionRemovedIn = sdkVersionRemovedIn;
    //        this.ModuleMovedTo = moduleMovedTo;
    //    }
    //}

    /// <summary>
    /// A class representing an api set or dll.
    /// </summary>
    internal class Module
    {
        public string Name { get; set; }
        public bool IsApiSet { get; set; }
        private List<FunctionWin32GroupedByModule> apis = new List<FunctionWin32GroupedByModule>();

        public System.Collections.ObjectModel.ReadOnlyCollection<FunctionWin32GroupedByModule> Apis
        {
            get { return new System.Collections.ObjectModel.ReadOnlyCollection<FunctionWin32GroupedByModule>(this.apis); }
        }

        public Module(string name, bool isApiSet)
        {
            this.Name = name;
            this.IsApiSet = isApiSet;
            this.apis = new List<FunctionWin32GroupedByModule>();
        }

        public void AddApi(string name, string sdkVersionIntroducedIn, string functionWin32Id)
        {
            this.apis.Add(new FunctionWin32GroupedByModule(this.Name, name, sdkVersionIntroducedIn, functionWin32Id));
        }

        public FunctionWin32GroupedByModule FindApi(string name)
        {
            return this.apis.Find(found => found.Name == name);
        }
    }

    ///// <summary>
    ///// A class representing apis grouped by initial char.
    ///// </summary>
    //internal class InitialCharGroup
    //{
    //    public string Name { get; set; }
    //    private List<FunctionWin32GroupedByInitialChar> apis = new List<FunctionWin32GroupedByInitialChar>();

    //    public System.Collections.ObjectModel.ReadOnlyCollection<FunctionWin32GroupedByInitialChar> Apis
    //    {
    //        get { return new System.Collections.ObjectModel.ReadOnlyCollection<FunctionWin32GroupedByInitialChar>(this.apis); }
    //    }

    //    public InitialCharGroup(string name)
    //    {
    //        this.Name = name;
    //        this.apis = new List<FunctionWin32GroupedByInitialChar>();
    //    }

    //    public void AddApi(string moduleName, string name, string sdkVersionIntroducedIn, string functionWin32Id, string sdkVersionRemovedIn, Module moduleMovedTo)
    //    {
    //        this.apis.Add(new FunctionWin32GroupedByInitialChar(moduleName, name, sdkVersionIntroducedIn, functionWin32Id, sdkVersionRemovedIn, moduleMovedTo));
    //    }

    //    public FunctionWin32GroupedByInitialChar FindApi(string name)
    //    {
    //        return this.apis.Find(found => found.Name == name);
    //    }

    //    public void Sort()
    //    {
    //        this.apis.Sort(new FunctionWin32GroupedByInitialCharComparer());
    //    }

    //    public static List<InitialCharGroup> ListOfInitialCharGroupFromListOfModule(List<Module> modules)
    //    {
    //        List<InitialCharGroup> initialCharGroups = new List<InitialCharGroup>();

    //        foreach (Module module in modules)
    //        {
    //            foreach (FunctionWin32GroupedByModule functionWin32GroupedByModule in module.Apis)
    //            {
    //                string initialCharGroupKey = functionWin32GroupedByModule.Name.Substring(0, 1);
    //                if (char.IsLetter(initialCharGroupKey[0]))
    //                {
    //                    initialCharGroupKey = initialCharGroupKey.ToUpper();
    //                }
    //                else
    //                {
    //                    initialCharGroupKey = "_";
    //                }
    //                InitialCharGroup initialCharGroup = initialCharGroups.Find(found => found.Name == initialCharGroupKey);
    //                if (initialCharGroup == null)
    //                {
    //                    initialCharGroup = new InitialCharGroup(initialCharGroupKey);
    //                    initialCharGroups.Add(initialCharGroup);
    //                }
    //                initialCharGroup.AddApi(functionWin32GroupedByModule.ModuleName, functionWin32GroupedByModule.Name, functionWin32GroupedByModule.SdkVersionIntroducedIn, functionWin32GroupedByModule.FunctionWin32Id, functionWin32GroupedByModule.SdkVersionRemovedIn, functionWin32GroupedByModule.ModuleMovedTo);
    //            }
    //        }

    //        foreach (InitialCharGroup initialCharGroup in initialCharGroups)
    //        {
    //            initialCharGroup.Sort();
    //        }
    //        initialCharGroups.Sort(new InitialCharGroupComparer());

    //        return initialCharGroups;
    //    }
    //}

    /// <summary>
    /// A class representing the API Sets in an umbrella lib.
    /// </summary>
    internal class UmbrellaLib
    {
        public string Name { get; set; }
        public List<Module> Modules = new List<Module>();
        //public List<InitialCharGroup> InitialCharGroups = null;

        public Dictionary<string, List<string>> setToApiMappings = new Dictionary<string, List<string>>();

        public UmbrellaLib(string name)
        {
            this.Name = name;
        }

        public Module GetModuleForApiName(string apiName)
        {
            foreach (Module module in this.Modules)
            {
                FunctionWin32Grouped apiToAdd = module.FindApi(apiName);
                if (apiToAdd != null)
                {
                    return module;
                }
            }
            return null;
        }

#if (USING_WCD)
		public void AddApi(Microsoft.CoreSystem.WindowsCompositionDatabase.Database.Api api, string sdkVersion, string functionWin32Id)
		{
			bool isApiSet = false;
			foreach (Microsoft.CoreSystem.WindowsCompositionDatabase.Database.Apiset apiset in api.Apisets) // api.Apisets can contain many entries: 6 is not unusual.
			{
				isApiSet = true; break;
			}

			Module module = this.Modules.Find(found => found.Name == api.Binary.Name);
			if (module == null)
			{
				module = new Module(api.Binary.Name, isApiSet);
				this.Modules.Add(module);
			}

			FunctionWin32Grouped apiToAdd = module.FindApi(api.Name);

			if (apiToAdd == null)
			{
				module.AddApi(api.Name, sdkVersion, functionWin32Id);
			}
		}
#endif

        public void SortAlphabetically()
        {
            //this.InitialCharGroups = InitialCharGroup.ListOfInitialCharGroupFromListOfModule(this.Modules);
        }
    }
}