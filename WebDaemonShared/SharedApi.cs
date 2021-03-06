﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Monsterer.Request;
using MetaData;
using ApiHost;
using WebDaemonSharedTables;

namespace WebDaemonShared
{
	public class SharedApi<T>
	{
		MySqlData m_database;

		public SharedApi(MySqlData database)
		{
			m_database = database;
		}
			/// <summary>	Executes the get market action. </summary>
		///
		/// <remarks>	Paul, 11/02/2015. </remarks>
		///
		/// <exception cref="ApiExceptionUnknownMarket">	Thrown when an API exception unknown market
		/// 												error condition occurs. </exception>
		///
		/// <param name="ctx">  	The context. </param>
		/// <param name="dummy">	The dummy. </param>
		///
		/// <returns>	A Task. </returns>
		public Task OnGetMarket(RequestContext ctx, T dummy)
		{
			string symbolPair = RestHelpers.GetPostArg<string, ApiExceptionMissingParameter>(ctx, WebForms.kSymbolPair);

			MarketRow market = m_database.GetMarket(symbolPair);
			if (market == null)
			{
				throw new ApiExceptionUnknownMarket(symbolPair);
			}
			else
			{
				ctx.Respond<MarketRow>(market);
			}

			return null;
		}

		/// <summary>	Executes the get all markets action. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <param name="ctx">  	The context. </param>
		/// <param name="dummy">	The dummy. </param>
		///
		/// <returns>	A Task. </returns>
		public Task OnGetAllMarkets(RequestContext ctx, T dummy)
		{
			ctx.Respond<List<MarketRow>>(m_database.GetAllMarkets());
			return null;
		}

		/// <summary>	Executes the get order status action. </summary>
		///
		/// <remarks>	Paul, 05/02/2015. </remarks>
		///
		/// <exception cref="ApiExceptionOrderNotFound">	Thrown when an API exception order not found
		/// 												error condition occurs. </exception>
		///
		/// <param name="ctx">  	The context. </param>
		/// <param name="dummy">	The dummy. </param>
		///
		/// <returns>	A Task. </returns>
		public Task OnGetOrderStatus(RequestContext ctx, T dummy)
		{
			string txid = RestHelpers.GetPostArg<string, ApiExceptionMissingParameter>(ctx, WebForms.kTxId);

			TransactionsRow t = m_database.GetTransaction(txid);
			if (t==null)
			{
				throw new ApiExceptionOrderNotFound(txid);
			}

			ctx.Respond<TransactionsRow>(t);
			return null;
		}

		/// <summary>	Executes the get transactions action. </summary>
		///
		/// <remarks>	Paul, 06/02/2015. </remarks>
		///
		/// <param name="ctx">  	The context. </param>
		/// <param name="dummy">	The dummy. </param>
		///
		/// <returns>	A Task. </returns>
		public Task OnGetLastTransactions(RequestContext ctx, T dummy)
		{
			uint limit = RestHelpers.GetPostArg<uint, ApiExceptionMissingParameter>(ctx, WebForms.kLimit);
			string market = RestHelpers.GetPostArg<string>(ctx, WebForms.kSymbolPair);

			ctx.Respond<List<TransactionsRowNoUid>>(m_database.GetLastTransactions(limit, market));
			return null;
		}

		/// <summary>	Executes the get my last transactions action. </summary>
		///
		/// <remarks>	Paul, 11/02/2015. </remarks>
		///
		/// <param name="ctx">  	The context. </param>
		/// <param name="dummy">	The dummy. </param>
		///
		/// <returns>	A Task. </returns>
		public Task OnGetMyLastTransactions(RequestContext ctx, T dummy)
		{
			uint limit = RestHelpers.GetPostArg<uint, ApiExceptionMissingParameter>(ctx, WebForms.kLimit);
			string memo = RestHelpers.GetPostArg<string>(ctx, WebForms.kMemo);
			string depositAddress = RestHelpers.GetPostArg<string>(ctx, WebForms.kDepositAddress);

			ctx.Respond<List<TransactionsRowNoUid>>(m_database.GetLastTransactionsFromDeposit(memo, depositAddress, limit));
			return null;
		}

		/// <summary>	Executes the get all transactions since action. </summary>
		///
		/// <remarks>	Paul, 20/02/2015. </remarks>
		///
		/// <param name="ctx">  	The context. </param>
		/// <param name="dummy">	The dummy. </param>
		///
		/// <returns>	A Task. </returns>
		public Task OnGetAllTransactionsSinceInternal(RequestContext ctx, T dummy)
		{
			uint tid = RestHelpers.GetPostArg<uint>(ctx, WebForms.kSince);
			ctx.Respond<List<TransactionsRow>>(m_database.GetAllTransactionsSince(tid));
			return null;
		}
	}
}
