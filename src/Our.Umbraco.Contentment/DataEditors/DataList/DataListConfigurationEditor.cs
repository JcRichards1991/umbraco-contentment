﻿/* Copyright © 2019 Lee Kelleher, Umbrella Inc and other contributors.
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Umbraco.Core.Composing;
using Umbraco.Core.IO;
using Umbraco.Core.PropertyEditors;
using Umbraco.Core.Serialization;

namespace Our.Umbraco.Contentment.DataEditors
{
    public class DataListConfigurationEditor : ConfigurationEditor
    {
        public DataListConfigurationEditor()
            : base()
        {
            var defaultConfigEditorConfig = new Dictionary<string, object>
            {
                { Constants.Conventions.ConfigurationEditors.MaxItems, 1 },
                { Constants.Conventions.ConfigurationEditors.DisableSorting, Constants.Values.True },
                { "overlaySize", "large" },
            };

            var dataSources = GetDataSources();
            var listEditors = GetListEditors();

            Fields.Add(
                "dataSource",
                "Data source",
                "Select and configure the data source.",
                IOHelper.ResolveUrl(ConfigurationEditorDataEditor.DataEditorViewPath),
                new Dictionary<string, object>(defaultConfigEditorConfig)
                {
                    { Constants.Conventions.ConfigurationEditors.Items, dataSources }
                });

            Fields.Add(
                "listEditor",
                "List editor",
                "Select and configure the type of editor for the data list.",
                IOHelper.ResolveUrl(ConfigurationEditorDataEditor.DataEditorViewPath),
                new Dictionary<string, object>(defaultConfigEditorConfig)
                {
                    { Constants.Conventions.ConfigurationEditors.Items, listEditors }
                });

            Fields.AddHideLabel();
        }

        public override IDictionary<string, object> ToValueEditor(object configuration)
        {
            var config = base.ToValueEditor(configuration);

            if (config.TryGetValue("dataSource", out var dataSource) && dataSource is JArray array && array.Count > 0)
            {
                // TODO: Review this, make it bulletproof

                var item = array[0];
                var type = TypeFinder.GetTypeByName(item["type"].ToString());
                if (type != null)
                {
                    var serializer = JsonSerializer.CreateDefault(new JsonSerializerSettings
                    {
                        ContractResolver = new ConfigurationFieldContractResolver(),
                        Converters = new List<JsonConverter>(new[] { new FuzzyBooleanConverter() })
                    });

                    var source = item["value"].ToObject(type, serializer) as IDataListSource;
                    var options = source?.GetItems() ?? new Dictionary<string, string>();

                    config.Add("items", options.Select(x => new { name = x.Value, value = x.Key }));
                }

                config.Remove("dataSource");
            }

            if (config.ContainsKey("listEditor"))
            {
                config.Remove("listEditor");
            }

            return config;
        }

        private IEnumerable<ConfigurationEditorModel> GetDataSources()
        {
            return ConfigurationEditorConfigurationEditor.GetConfigurationEditors<IDataListSource>();
        }

        private IEnumerable<ConfigurationEditorModel> GetListEditors()
        {
            return ConfigurationEditorConfigurationEditor.GetConfigurationEditors<IDataListEditor>();
        }
    }
}