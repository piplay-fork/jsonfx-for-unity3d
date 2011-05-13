using System;
using System.Text;
using System.Web.Mvc;
using System.Xml;
using System.Xml.Serialization;

using JsonFx.Json;
using JsonFx.Xml;
using Ninject.Modules;
using Ninject.Web.Mvc;

namespace JbstOnline.Mvc.IoC
{
	/// <summary>
	/// Establishes IoC bindings between DI interfaces and their implementations.
	/// </summary>
	public class AppIocModule : NinjectModule
	{
		#region NinjectModule Members

		public override void Load()
		{
			// serialization
			this.Bind<IDataWriter>().To<JsonDataWriter>().InSingletonScope();
			this.Bind<JsonWriterSettings>().ToConstant(JsonDataWriter.CreateSettings(false));
			this.Bind<IDataWriter>().To<XmlDataWriter>().InSingletonScope();
			this.Bind<XmlWriterSettings>().ToConstant(XmlDataWriter.CreateSettings(Encoding.UTF8, false));
			this.Bind<XmlSerializerNamespaces>().ToConstant(new XmlSerializerNamespaces());
			this.Bind<IDataWriterProvider>().To<DataWriterProvider>().InSingletonScope();

			this.Bind<IDataReader>().To<JsonDataReader>().InSingletonScope();
			this.Bind<JsonReaderSettings>().ToConstant(JsonDataReader.CreateSettings(true));
			this.Bind<IDataReader>().To<XmlDataReader>().InSingletonScope();
			this.Bind<XmlReaderSettings>().ToConstant(XmlDataReader.CreateSettings());
			this.Bind<IDataReaderProvider>().To<DataReaderProvider>().InSingletonScope();

			// MVC and IoC types
			this.Bind<IActionInvoker>().To<NinjectActionInvoker>().InTransientScope();
		}

		#endregion NinjectModule Members
	}
}
