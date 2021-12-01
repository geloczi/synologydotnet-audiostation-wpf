using System;
using System.IO;
using System.Reflection;

namespace Utils
{
    /// <summary>
    /// Gets details about an Assembly. All properties of this class are lazy-loaded.
    /// </summary>
    public class AssemblyProps
    {
        #region Static Properties
        private static readonly object _entryAssemblyDetailsLock = new object();
        private static AssemblyProps _entryAssemblyDetails;

        /// <summary>
        /// Gets the entry Assembly Details
        /// </summary>
        public static AssemblyProps EntryAssembly
        {
            get
            {
                lock (_entryAssemblyDetailsLock)
                {
                    if (_entryAssemblyDetails is null)
                        _entryAssemblyDetails = new AssemblyProps(Assembly.GetEntryAssembly());
                }
                return _entryAssemblyDetails;
            }
        }
        #endregion

        #region Fields
        private readonly object _lock = new object();
        private readonly Assembly _assembly;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the full name
        /// </summary>
        public string FullName => _assembly.FullName;

        /// <summary>
        /// Gets the full file path of the assembly
        /// </summary>
        public string Location => _assembly.Location;

        private string _fileName;
        /// <summary>
        /// Gets the file name
        /// </summary>
        public string FileName
        {
            get
            {
                lock (_lock)
                {
                    if (_fileName is null)
                        _fileName = Path.GetFileName(_assembly.Location);
                }
                return _fileName;
            }
        }

        private string _title;
        /// <summary>
        /// Gets the title
        /// </summary>
        public string Title
        {
            get
            {
                lock (_lock)
                {
                    if (_title is null)
                        _title = GetTitle(_assembly);
                }
                return _title;
            }
        }

        private Version _version;
        /// <summary>
        /// Gets the version number
        /// </summary>
        public Version Version
        {
            get
            {
                lock (_lock)
                {
                    if (_version is null)
                        _version = GetVersion(_assembly);
                }
                return _version;
            }
        }

        private string _description;
        /// <summary>
        /// Gets the description
        /// </summary>
        public string Description
        {
            get
            {
                lock (_lock)
                {
                    if (_description is null)
                        _description = GetDescription(_assembly);
                }
                return _description;
            }
        }

        private string _product;
        /// <summary>
        /// Gets the product
        /// </summary>
        public string Product
        {
            get
            {
                lock (_lock)
                {
                    if (_product is null)
                        _product = GetProduct(_assembly);
                }
                return _product;
            }
        }

        private string _productGuid;
        /// <summary>
        /// Gets the product guid
        /// </summary>
        public string ProductGuid
        {
            get
            {
                lock (_lock)
                {
                    if (_productGuid is null)
                        _productGuid = GetProductGuid(_assembly);
                }
                return _productGuid;
            }
        }

        private string _copyright;
        /// <summary>
        /// Gets the copyright text
        /// </summary>
        public string Copyright
        {
            get
            {
                lock (_lock)
                {
                    if (_copyright is null)
                        _copyright = GetCopyright(_assembly);
                }
                return _copyright;
            }
        }

        private string _company;
        /// <summary>
        /// Gets the company
        /// </summary>
        public string Company
        {
            get
            {
                lock (_lock)
                {
                    if (_company is null)
                        _company = GetCompany(_assembly);
                }
                return _company;
            }
        }

        private DateTime? _buildDateTimeUtc;
        /// <summary>
        /// Gets the build date from the Assembly in UTC
        /// </summary>
        public DateTime BuildDateTimeUtc
        {
            get
            {
                lock (_lock)
                {
                    if (!_buildDateTimeUtc.HasValue)
                        _buildDateTimeUtc = GetBuildDateTimeUtc(_assembly);
                }
                return _buildDateTimeUtc.Value;
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="assembly">The assembly to get the details from</param>
        public AssemblyProps(Assembly assembly)
        {
            _assembly = assembly;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Returns the FullName of the assembly
        /// </summary>
        /// <returns></returns>
        public override string ToString() => FullName;
        #endregion

        #region Public Static Methods
        /// <summary>
        /// Gets the build date from the Assembly in UTC
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public static DateTime GetBuildDateTimeUtc(Assembly a)
        {
            try
            {
                return File.GetLastWriteTimeUtc(a.Location);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// Gets the assembly title.
        /// </summary>
        public static string GetTitle(Assembly a)
        {
            object[] attributes = a.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
            if (attributes.Length > 0)
            {
                AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                if (!string.IsNullOrEmpty(titleAttribute.Title))
                    return titleAttribute.Title;
            }
            return Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
        }

        /// <summary>
        /// Gets the assembly version.
        /// </summary>
        public static Version GetVersion(Assembly a)
        {
            return a.GetName().Version;
        }

        /// <summary>
        /// Gets the assembly description.
        /// </summary>
        public static string GetDescription(Assembly a)
        {
            object[] attributes = a.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
            if (attributes.Length == 0)
                return string.Empty;
            return ((AssemblyDescriptionAttribute)attributes[0]).Description;
        }

        /// <summary>
        /// Gets the assembly product.
        /// </summary>
        public static string GetProduct(Assembly a)
        {
            object[] attributes = a.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
            if (attributes.Length == 0)
                return string.Empty;
            return ((AssemblyProductAttribute)attributes[0]).Product;
        }

        /// <summary>
        /// Gets the assembly product.
        /// </summary>
        public static string GetProductGuid(Assembly a)
        {
            object[] attributes = a.GetCustomAttributes(typeof(System.Runtime.InteropServices.GuidAttribute), false);
            if (attributes.Length == 0)
                return string.Empty;
            return ((System.Runtime.InteropServices.GuidAttribute)attributes[0]).Value;
        }

        /// <summary>
        /// Gets the assembly copyright.
        /// </summary>
        public static string GetCopyright(Assembly a)
        {
            object[] attributes = a.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
            if (attributes.Length == 0)
                return string.Empty;
            return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
        }

        /// <summary>
        /// Gets the assembly company.
        /// </summary>
        public static string GetCompany(Assembly a)
        {
            object[] attributes = a.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
            if (attributes.Length == 0)
                return string.Empty;
            return ((AssemblyCompanyAttribute)attributes[0]).Company;
        }
        #endregion
    }
}