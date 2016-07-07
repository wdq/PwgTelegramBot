﻿#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace PwgTelegramBot
{
	using System.Data.Linq;
	using System.Data.Linq.Mapping;
	using System.Data;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Linq;
	using System.Linq.Expressions;
	using System.ComponentModel;
	using System;
	
	
	[global::System.Data.Linq.Mapping.DatabaseAttribute(Name="PwgTelegramBot")]
	public partial class BotDatabaseDataContext : System.Data.Linq.DataContext
	{
		
		private static System.Data.Linq.Mapping.MappingSource mappingSource = new AttributeMappingSource();
		
    #region Extensibility Method Definitions
    partial void OnCreated();
    partial void InsertUserState(UserState instance);
    partial void UpdateUserState(UserState instance);
    partial void DeleteUserState(UserState instance);
    partial void InsertHarvestAuth(HarvestAuth instance);
    partial void UpdateHarvestAuth(HarvestAuth instance);
    partial void DeleteHarvestAuth(HarvestAuth instance);
    #endregion
		
		public BotDatabaseDataContext() : 
				base(global::System.Configuration.ConfigurationManager.ConnectionStrings["PwgTelegramBotConnectionString"].ConnectionString, mappingSource)
		{
			OnCreated();
		}
		
		public BotDatabaseDataContext(string connection) : 
				base(connection, mappingSource)
		{
			OnCreated();
		}
		
		public BotDatabaseDataContext(System.Data.IDbConnection connection) : 
				base(connection, mappingSource)
		{
			OnCreated();
		}
		
		public BotDatabaseDataContext(string connection, System.Data.Linq.Mapping.MappingSource mappingSource) : 
				base(connection, mappingSource)
		{
			OnCreated();
		}
		
		public BotDatabaseDataContext(System.Data.IDbConnection connection, System.Data.Linq.Mapping.MappingSource mappingSource) : 
				base(connection, mappingSource)
		{
			OnCreated();
		}
		
		public System.Data.Linq.Table<UserState> UserStates
		{
			get
			{
				return this.GetTable<UserState>();
			}
		}
		
		public System.Data.Linq.Table<HarvestAuth> HarvestAuths
		{
			get
			{
				return this.GetTable<HarvestAuth>();
			}
		}
	}
	
	[global::System.Data.Linq.Mapping.TableAttribute(Name="dbo.UserState")]
	public partial class UserState : INotifyPropertyChanging, INotifyPropertyChanged
	{
		
		private static PropertyChangingEventArgs emptyChangingEventArgs = new PropertyChangingEventArgs(String.Empty);
		
		private int _UserId;
		
		private string _State;
		
		private System.Nullable<bool> _Approved;
		
		private string _Notes;
		
		private System.Nullable<bool> _IsAdmin;
		
		private System.Nullable<bool> _IsStateTextEntry;
		
    #region Extensibility Method Definitions
    partial void OnLoaded();
    partial void OnValidate(System.Data.Linq.ChangeAction action);
    partial void OnCreated();
    partial void OnUserIdChanging(int value);
    partial void OnUserIdChanged();
    partial void OnStateChanging(string value);
    partial void OnStateChanged();
    partial void OnApprovedChanging(System.Nullable<bool> value);
    partial void OnApprovedChanged();
    partial void OnNotesChanging(string value);
    partial void OnNotesChanged();
    partial void OnIsAdminChanging(System.Nullable<bool> value);
    partial void OnIsAdminChanged();
    partial void OnIsStateTextEntryChanging(System.Nullable<bool> value);
    partial void OnIsStateTextEntryChanged();
    #endregion
		
		public UserState()
		{
			OnCreated();
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_UserId", DbType="Int NOT NULL", IsPrimaryKey=true)]
		public int UserId
		{
			get
			{
				return this._UserId;
			}
			set
			{
				if ((this._UserId != value))
				{
					this.OnUserIdChanging(value);
					this.SendPropertyChanging();
					this._UserId = value;
					this.SendPropertyChanged("UserId");
					this.OnUserIdChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_State", DbType="NVarChar(MAX)")]
		public string State
		{
			get
			{
				return this._State;
			}
			set
			{
				if ((this._State != value))
				{
					this.OnStateChanging(value);
					this.SendPropertyChanging();
					this._State = value;
					this.SendPropertyChanged("State");
					this.OnStateChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_Approved", DbType="Bit")]
		public System.Nullable<bool> Approved
		{
			get
			{
				return this._Approved;
			}
			set
			{
				if ((this._Approved != value))
				{
					this.OnApprovedChanging(value);
					this.SendPropertyChanging();
					this._Approved = value;
					this.SendPropertyChanged("Approved");
					this.OnApprovedChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_Notes", DbType="NVarChar(MAX)")]
		public string Notes
		{
			get
			{
				return this._Notes;
			}
			set
			{
				if ((this._Notes != value))
				{
					this.OnNotesChanging(value);
					this.SendPropertyChanging();
					this._Notes = value;
					this.SendPropertyChanged("Notes");
					this.OnNotesChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_IsAdmin", DbType="Bit")]
		public System.Nullable<bool> IsAdmin
		{
			get
			{
				return this._IsAdmin;
			}
			set
			{
				if ((this._IsAdmin != value))
				{
					this.OnIsAdminChanging(value);
					this.SendPropertyChanging();
					this._IsAdmin = value;
					this.SendPropertyChanged("IsAdmin");
					this.OnIsAdminChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_IsStateTextEntry", DbType="Bit")]
		public System.Nullable<bool> IsStateTextEntry
		{
			get
			{
				return this._IsStateTextEntry;
			}
			set
			{
				if ((this._IsStateTextEntry != value))
				{
					this.OnIsStateTextEntryChanging(value);
					this.SendPropertyChanging();
					this._IsStateTextEntry = value;
					this.SendPropertyChanged("IsStateTextEntry");
					this.OnIsStateTextEntryChanged();
				}
			}
		}
		
		public event PropertyChangingEventHandler PropertyChanging;
		
		public event PropertyChangedEventHandler PropertyChanged;
		
		protected virtual void SendPropertyChanging()
		{
			if ((this.PropertyChanging != null))
			{
				this.PropertyChanging(this, emptyChangingEventArgs);
			}
		}
		
		protected virtual void SendPropertyChanged(String propertyName)
		{
			if ((this.PropertyChanged != null))
			{
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}
	
	[global::System.Data.Linq.Mapping.TableAttribute(Name="dbo.HarvestAuth")]
	public partial class HarvestAuth : INotifyPropertyChanging, INotifyPropertyChanged
	{
		
		private static PropertyChangingEventArgs emptyChangingEventArgs = new PropertyChangingEventArgs(String.Empty);
		
		private int _UserId;
		
		private string _HarvestCode;
		
		private string _HarvestToken;
		
		private System.Nullable<System.DateTime> _HarvestTokenExpiration;
		
		private string _HarvestRefreshToken;
		
    #region Extensibility Method Definitions
    partial void OnLoaded();
    partial void OnValidate(System.Data.Linq.ChangeAction action);
    partial void OnCreated();
    partial void OnUserIdChanging(int value);
    partial void OnUserIdChanged();
    partial void OnHarvestCodeChanging(string value);
    partial void OnHarvestCodeChanged();
    partial void OnHarvestTokenChanging(string value);
    partial void OnHarvestTokenChanged();
    partial void OnHarvestTokenExpirationChanging(System.Nullable<System.DateTime> value);
    partial void OnHarvestTokenExpirationChanged();
    partial void OnHarvestRefreshTokenChanging(string value);
    partial void OnHarvestRefreshTokenChanged();
    #endregion
		
		public HarvestAuth()
		{
			OnCreated();
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_UserId", DbType="Int NOT NULL", IsPrimaryKey=true)]
		public int UserId
		{
			get
			{
				return this._UserId;
			}
			set
			{
				if ((this._UserId != value))
				{
					this.OnUserIdChanging(value);
					this.SendPropertyChanging();
					this._UserId = value;
					this.SendPropertyChanged("UserId");
					this.OnUserIdChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_HarvestCode", DbType="NVarChar(MAX) NOT NULL", CanBeNull=false)]
		public string HarvestCode
		{
			get
			{
				return this._HarvestCode;
			}
			set
			{
				if ((this._HarvestCode != value))
				{
					this.OnHarvestCodeChanging(value);
					this.SendPropertyChanging();
					this._HarvestCode = value;
					this.SendPropertyChanged("HarvestCode");
					this.OnHarvestCodeChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_HarvestToken", DbType="NVarChar(MAX)")]
		public string HarvestToken
		{
			get
			{
				return this._HarvestToken;
			}
			set
			{
				if ((this._HarvestToken != value))
				{
					this.OnHarvestTokenChanging(value);
					this.SendPropertyChanging();
					this._HarvestToken = value;
					this.SendPropertyChanged("HarvestToken");
					this.OnHarvestTokenChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_HarvestTokenExpiration", DbType="DateTime2")]
		public System.Nullable<System.DateTime> HarvestTokenExpiration
		{
			get
			{
				return this._HarvestTokenExpiration;
			}
			set
			{
				if ((this._HarvestTokenExpiration != value))
				{
					this.OnHarvestTokenExpirationChanging(value);
					this.SendPropertyChanging();
					this._HarvestTokenExpiration = value;
					this.SendPropertyChanged("HarvestTokenExpiration");
					this.OnHarvestTokenExpirationChanged();
				}
			}
		}
		
		[global::System.Data.Linq.Mapping.ColumnAttribute(Storage="_HarvestRefreshToken", DbType="NVarChar(MAX)")]
		public string HarvestRefreshToken
		{
			get
			{
				return this._HarvestRefreshToken;
			}
			set
			{
				if ((this._HarvestRefreshToken != value))
				{
					this.OnHarvestRefreshTokenChanging(value);
					this.SendPropertyChanging();
					this._HarvestRefreshToken = value;
					this.SendPropertyChanged("HarvestRefreshToken");
					this.OnHarvestRefreshTokenChanged();
				}
			}
		}
		
		public event PropertyChangingEventHandler PropertyChanging;
		
		public event PropertyChangedEventHandler PropertyChanged;
		
		protected virtual void SendPropertyChanging()
		{
			if ((this.PropertyChanging != null))
			{
				this.PropertyChanging(this, emptyChangingEventArgs);
			}
		}
		
		protected virtual void SendPropertyChanged(String propertyName)
		{
			if ((this.PropertyChanged != null))
			{
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}
}
#pragma warning restore 1591
