﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YouTrackSharp.Json;

namespace YouTrackSharp.Issues
{
    // TODO: Add dynamic object implementation cache so no iteration is needed over Fields

    /// <summary>
    /// A class that represents YouTrack issue information. Can be casted to a <see cref="DynamicObject"/>.
    /// </summary>
    [DebuggerDisplay("{Id}: {Summary}")]
    public class Issue 
        : DynamicObject
    {
        private readonly IDictionary<string, Field> _fields = new Dictionary<string, Field>(StringComparer.OrdinalIgnoreCase);
        
        /// <summary>
        /// Creates an instance of the <see cref="Issue" /> class.
        /// </summary>
        public Issue()
        {
            Comments = new List<Comment>();
            Tags = new List<SubValue>();
        }

        /// <summary>
        /// Id of the issue.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Entity Id internal to YouTrack.
        /// </summary>
        [JsonProperty("entityId")]
        public string EntityId { get; set; }

        /// <summary>
        /// If issue was imported from JIRA, represents the Id it has in JIRA.
        /// </summary>
        [JsonProperty("jiraId")]
        public string JiraId { get; set; }

        /// <summary>
        /// Summary of the issue.
        /// </summary>
        public string Summary {
            get
            {
                var field = GetField("Summary");
                return field?.Value.ToString();
            }
            set
            {
                var field = GetField("Summary");
                if (field != null)
                {
                    field.Value = value;
                }
                else
                {
                    _fields.Add("Summary", new Field { Name = "Summary", Value = value });
                }
            }
        }

        /// <summary>
        /// Description of the issue.
        /// </summary>
        public string Description {
            get
            {
                var field = GetField("Description");
                return field?.Value.ToString();
            }
            set
            {
                var field = GetField("Description");
                if (field != null)
                {
                    field.Value = value;
                }
                else
                {
                    _fields.Add("Description", new Field { Name = "Description", Value = value });
                }
            }
        }
        
        /// <summary>
        /// Issue fields.
        /// </summary>
        public ICollection<Field> Fields
        {
            get { return _fields.Values; } 
        }

        /// <summary>
        /// Issue comments.
        /// </summary>
        [JsonProperty("comment")]
        public ICollection<Comment> Comments { get; set; }

        /// <summary>
        /// Issue tags.
        /// </summary>
        [JsonProperty("tag")]
        public ICollection<SubValue> Tags { get; set; }

        /// <summary>
        /// Gets a specific <see cref="Field"/> from the <see cref="Fields"/> collection.
        /// </summary>
        /// <param name="fieldName">The name of the <see cref="Field"/> to get.</param>
        /// <returns><see cref="Field"/> matching the <paramref name="fieldName"/>; null when not found.</returns>
        public Field GetField(string fieldName)
        {
            Field field;
            _fields.TryGetValue(fieldName, out field);
            return field;
        }
        
        /// <inheritdoc />
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var field = GetField(binder.Name) 
                ?? GetField(binder.Name.Replace("_", " ")); // support fields with space in the name by using underscore in code
            
            if (field != null)
            {
                result = field.Value;
                return true;
            }

            result = null;
            return true;
        }

        /// <inheritdoc />
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            // "field" setter when deserializing JSON into Issue object
            if (string.Equals(binder.Name, "field", StringComparison.OrdinalIgnoreCase) && value is JArray)
            {   
                var fieldElements = ((JArray)value).ToObject<List<Field>>();
                foreach (var fieldElement in fieldElements)
                {
                    if (fieldElement.Value is JArray fieldElementAsArray)
                    {
                        // Map collection
                        if (string.Equals(fieldElement.Name, "assignee", StringComparison.OrdinalIgnoreCase))
                        {
                            // For assignees, we can do a strong-typed list.
                            fieldElement.Value = fieldElementAsArray.ToObject<List<Assignee>>();
                        }
                        else
                        {
                            if (fieldElementAsArray.First is JValue &&
                                JTokenTypeUtil.IsSimpleType(fieldElementAsArray.First.Type))
                            {
                                // Map simple arrays to a collection of string
                                fieldElement.Value = fieldElementAsArray.ToObject<List<string>>();
                            }
                            else
                            {
                                // Map more complex arrays to JToken[]
                                fieldElement.Value = fieldElementAsArray;
                            }
                        }
                    }
                    
                    // Set the actual field
                    _fields[fieldElement.Name] = fieldElement;
                }
             
                return true;
            }
            
            // Regular setter
            Field field;
            if (_fields.TryGetValue(binder.Name, out field) || _fields.TryGetValue(binder.Name.Replace("_", " "), out field))
            {
                field.Value = value;
            }
            else
            {
                _fields.Add(binder.Name, new Field { Name = binder.Name, Value = value });
            }
            
            return true;
        }

        /// <summary>
        /// Returns the current <see cref="Issue" /> as a <see cref="DynamicObject" />.
        /// </summary>
        /// <returns>The current <see cref="Issue" /> as a <see cref="DynamicObject" />.</returns>
        public dynamic AsDynamic()
        {
            return this;
        }
    }
}