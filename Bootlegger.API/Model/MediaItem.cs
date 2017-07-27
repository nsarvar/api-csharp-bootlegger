/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bootleg.API
{

    [Table("Uploads")]
    public class MediaItem
    {

        public string titletext { get; set; }

        public class Meta_Dic
        {
            public Dictionary<string, string> static_meta { get; set; }
            public Dictionary<string, string> timed_meta { get; set; }
            public JObject role_ex { get; set; }
            public JObject shot_ex { get; set; }
            public JObject phase_ex { get; set; }
        }

        [Ignore]
        public JObject user { set { Contributor = (value["profile"] as JObject)["displayName"].ToString(); } }

        [Ignore]
        public Shot.ShotTypes MediaType { get {
                if (!string.IsNullOrEmpty(titletext))
                    return Shot.ShotTypes.TITLE;

                if (Static_Meta.ContainsKey("media_type"))
                    return (Shot.ShotTypes)Enum.Parse(typeof(Shot.ShotTypes), Static_Meta["media_type"].ToString());
                else
                    return Shot.ShotTypes.VIDEO;
            }
            set
            {
                Static_Meta["media_type"] = value.ToString();
            }
        }

        public string Contributor { get; set; }

        public bool deleted;

        public MediaItem()
        {
            Static_Meta = new Dictionary<string, string>();
            TimedMeta = new Dictionary<string, string>();
            Status = MediaStatus.META_UPLOADED;
            path = "";
            //lowres = "";
        }

        //meta that comes back from the server:
        [Ignore]
        public Meta_Dic meta { get; set; }
        
        public string Meta_ser1
        {
            get
            {
                return JsonConvert.SerializeObject(meta);
            }
            set
            {
                if (value != null)
                    meta = JsonConvert.DeserializeObject<Meta_Dic>(value);
            }
        }

        public TimeSpan ClipLength { get
            {
                return TimeSpan.Parse( ( Static_Meta.ContainsKey("clip_length"))?Static_Meta["clip_length"]:"0:0:0");
            }
        }

        public bool IsOnServer { get; set; }

        public override string ToString()
        {
            return ShortName;
        }

        [Ignore]
        public double Progress { get;set;}
        public string FileSize { get; set; }

        public enum MediaStatus {NOTONSERVER, META_UPLOADED, RECORDING, READY, UPLOADING, DONE, FILEUPLOADERROR,FILENOTEXISTERROR, PROCESSING,PLACEHOLDER, CREATEMEDIAERROR };

        private MediaStatus _status;
        public int retrycount;
        public string path { get; set; }
        public string lowres { get; set; }
        private string _created_by;
        public string created_by {
            get { if (meta != null && meta.static_meta != null) return _created_by; else return (Static_Meta.ContainsKey("created_by"))? Static_Meta["created_by"]:""; }
                set{
                    _created_by = value;
                }
        }

        private string _event_id;
        public string event_id
        {
            get {
                if ((meta != null && meta.static_meta != null) || Static_Meta.Count == 0) return _event_id; else return (Static_Meta.ContainsKey("event_id")) ? Static_Meta["event_id"] : "";
            }
            set
            {
                _event_id = value;
            }
        }

        public MediaStatus Status
        {
            get {
                if (meta != null && !string.IsNullOrEmpty(path))//check for upload:
                    return MediaStatus.DONE;
                else
                    return _status;
            }
            set
            {
                _status = value;
                if (OnChanged != null)
                {
                    try
                    {
                        OnChanged(this);
                    }
                    catch (Exception e)
                    {
                        //TODO -- GET TO THE BOTTOM OF THIS EXCEPTION (DB RELATED)
                    }
                }
            }
        }

        public event Action<MediaItem> OnChanged;


		//public delegate void ProgressDelegate(MediaItem media, int p1, int p2, int p3);

		/// <summary>
		/// Occurs when media item upload progress changed.
		/// </summary>
        //public event ProgressDelegate OnProgress;



        internal void FireProgress()
        {
            if (OnChanged != null) OnChanged(this);
        }

        public string ShortName { get; set; }

        //todo upload progress event?

        public string Filename { get; set; }
        public string DummyName { get; set; }
        [Ignore]
        public string PrintName
        {
            get
            {
                if (DummyName != "" && DummyName != null)
                {
                    return DummyName;
                }
                else
                {
                    if (Filename != null)
                        return Filename.Split('/').Last().Replace(".mp4", "");
                    else
                        return id;
                }
            }
        }
        [PrimaryKey, Column("id")]
        public string id { get; set; }


        private Dictionary<string, string> _staticmeta = new Dictionary<string, string>();

        [Ignore]
        public Dictionary<string,string> Static_Meta {
            get {
                if (meta != null && meta.static_meta != null)
                    return meta.static_meta;
                else return _staticmeta;
            }
            set {
                _staticmeta = value; }
        }

        public string Meta_ser { 
            get { 
                //Console.WriteLine("ser: " + JsonConvert.SerializeObject(Meta));
                
                return JsonConvert.SerializeObject(Static_Meta);
            } 
            set { 
                if (value != null)
                    Static_Meta = JsonConvert.DeserializeObject<Dictionary<string, string>>(value); } 
        }

        [Ignore]
        public Dictionary<string,string> TimedMeta { get; set; }

        [Ignore]
        public string TimedMeta_ser { 
            get { 
            //Console.WriteLine("ser: " + JsonConvert.SerializeObject(TimedMeta));
            return JsonConvert.SerializeObject(TimedMeta);
            } 
            set
            {
                try
                {
                    if (value != null)
                        TimedMeta = JsonConvert.DeserializeObject<Dictionary<string, string>>(value);
                }
                catch
                {
                    //failed to deserialize
                }
            }
        }

        
        [JsonProperty(PropertyName = "thumb")]
        //[DataMember(Name = "thumb")]
        public string Thumb { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool ThumbOnServer { get; set; }


        //public string thumb { get; set; }
        //added for v3
        public TimeSpan inpoint { get; set; }
        public TimeSpan outpoint { get
            {
                if (_outpoint != TimeSpan.Zero)
                    return _outpoint;
                else
                    return ClipLength;
            }
            set
            {
                _outpoint = value;
            }
        }
        private TimeSpan _outpoint;

        public MediaItem Copy()
        {
            return MemberwiseClone() as MediaItem;
        }
    }
}

