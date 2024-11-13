//*******************************************************************
// ComTypeHelper.cs
// Author: Apostolos Grammatopoulos
//
// ComTypeHelper class returning the Type of a LUSAS COM object
//*******************************************************************

using System;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;

namespace LusasLookup
{
    public static class ComTypeHelper
    {
        #region Private Constants

        private const int S_OK = 0;
        private const int LOCALE_SYSTEM_DEFAULT = 2 << 10;

        #endregion
        
        #region Public Methods

        /// <summary>Returns the COM object type.</summary>
        /// <param name="comObject">Target COM object.</param>
        /// <returns>Instead of loading the object type through Marshal, the type is searched by GUID in the app assemblies for much faster loading.</returns>
        public static Type GetCOMObjectType(object comObject)
        {
            if (!Marshal.IsComObject(comObject)) throw new ArgumentException("This is not COM object.", "comObject");

            // Cast object to IDispatch
            IDispatch dispatch = (IDispatch)comObject;

            // Get object type GUID
            IntPtr typeInfoPtr;
            dispatch.GetTypeInfo(0, LOCALE_SYSTEM_DEFAULT, out typeInfoPtr);
            if (typeInfoPtr == IntPtr.Zero) throw new ArgumentNullException(nameof(comObject));
            ITypeInfo typeInfo = Marshal.GetTypedObjectForIUnknown(typeInfoPtr, typeof(ITypeInfo)) as ITypeInfo;
            Guid guid = GetTypeInfoGuid(typeInfo);

            // Loop app assemblies and search for GUID
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                // Focus on Lusas.Interop assembly
                if (a.ManifestModule.Name != "Lusas.Interop.dll") continue;
                foreach (Type t in a.GetTypes())
                {
                    if (t.IsInterface && t.IsImport && t.GUID == guid) return t;
                }
            }

            // Not found in assemblies, use generic way
            return Type.GetTypeFromCLSID(guid);
        }

        #endregion

        #region Private Methods

        internal static Func<ITypeInfo, Guid> GetTypeInfoGuid = (Func<ITypeInfo, Guid>)Delegate.CreateDelegate(typeof(Func<ITypeInfo, Guid>), typeof(Marshal).GetMethod("GetTypeInfoGuid", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(ITypeInfo) }, null), true);
        
        #endregion

        #region Private Interfaces

        [Guid("00020400-0000-0000-C000-000000000046"), ComImport(), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IDispatch
        {
            [PreserveSig]
            int GetTypeInfoCount(out int pctinfo);

            [PreserveSig]
            int GetTypeInfo(
                [MarshalAs(UnmanagedType.U4)] int iTInfo,
                [MarshalAs(UnmanagedType.U4)] int lcid,
                out IntPtr ppTInfo);

            [PreserveSig]
            int GetIDsOfNames(
                ref Guid riid,
                [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr)] string[] rgszNames,
                int cNames,
                int lcid,
                [MarshalAs(UnmanagedType.LPArray)] int[] rgDispId);

            [PreserveSig]
            int Invoke(
                int dispIdMember,
                ref Guid riid,
                uint lcid,
                ushort wFlags,
                ref System.Runtime.InteropServices.ComTypes.DISPPARAMS pDispParams,
                out object pVarResult,
                ref System.Runtime.InteropServices.ComTypes.EXCEPINFO pExcepInfo,
                IntPtr[] puArgErr);
        }

        #endregion
    }
}