﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Monsterer.Request;

using WebHost;
using WebHost.Components;
using WebHost.WebSystem;

using WebDaemonShared;
using WebDaemonSharedTables;
using MetaExchange;

namespace MetaExchange.Pages
{
	public class MainPage : SharedPage
	{
		public override Task Render(RequestContext ctx, StringWriter stream, IDummy authObj)
		{
			#if MONO
			AddResource(new JsResource(Constants.kWebRoot, "/js/mainPageCompiled.js", true));
			#endif

			//AddResource(new MetaResource("bitsharesAccount", authObj.m_bitsharesAccount));

			// render head
			base.Render(ctx, stream, authObj);

			using (new DivContainer(stream, "ng-app", "myApp", "ng-controller", "StatsController"))
			{
				using (new DivContainer(stream, HtmlAttributes.@class, "jumbotron clearfix"))
				{
					using (new DivContainer(stream, HtmlAttributes.@class, "container"))
					{
						using (new DivContainer(stream, HtmlAttributes.@class, "row"))
						{
							using (new DivContainer(stream, HtmlAttributes.@class, "col-xs-12"))
							{
								BaseComponent.SPAN(stream, "Metaexchange<sup>beta</sup>", HtmlAttributes.@class, "noTopMargin h1");

								P("The place to buy and sell bitBTC.");

								using (new DivContainer(stream, HtmlAttributes.id, "serviceStatusId"))
								{
									SPAN("Service status: ");
									SPAN("{{status}}", "", "label label-{{label}}");
								}
							}
						}
					}
				}

				//
				// buy and sell section
				//
				// 
				using (new DivContainer(stream, HtmlAttributes.@class, "container"))
				{
					using (new DivContainer(stream, HtmlAttributes.@class, "row"))
					{
						using (new DivContainer(stream, HtmlAttributes.@class, "col-sm-6"))
						{
							Button("Buy bitBTC<br/><span class='badge'>1.00 BTC</span><span class='glyphicon glyphicon-arrow-right arrow'></span><span class='badge'>{{1/market.ask | number:3}} bitBTC</span>", HtmlAttributes.@class, "btn btn-success btn-lg btn-block",
													"data-toggle", "collapse",
													"aria-expanded", "false",
													"aria-controls", "buyId",
													"data-target", "#buyId");

							
						}
						using (new DivContainer(stream, HtmlAttributes.@class, "col-sm-6"))
						{
							Button("Sell bitBTC<br/><span class='badge'>1.00 bitBTC</span><span class='glyphicon glyphicon-arrow-right arrow'></span><span class='badge'>{{market.bid | number:3}} BTC</span>", HtmlAttributes.@class, "btn btn-danger btn-lg btn-block",
													"data-toggle", "collapse",
													"aria-expanded", "false",
													"aria-controls", "sellId",
													"data-target", "#sellId");
						}
					}
					using (new DivContainer(stream, HtmlAttributes.@class, "row"))
					{
						using (new DivContainer(stream, HtmlAttributes.@class, "col-sm-6"))
						{
							using (new Panel(stream, "Buy bitBTC", "panel panel-success collapse", false, "buyId"))
							{
								P("Once you enter your bitshares account name, we will generate your deposit address and send your bitBTC to you after 1 confirmation.");

								using (var fm = new FormContainer(stream, HtmlAttributes.method, "post",
																			HtmlAttributes.ajax, true,
																			HtmlAttributes.handler, "OnSubmitAddressBts",
																			HtmlAttributes.action, Routes.kSubmitAddress))
								{
									using (new DivContainer(stream, HtmlAttributes.@class, "form-group"))
									{
										fm.Label(stream, "Where shall we send your bitBTC?");

										fm.Input(stream, HtmlAttributes.type, InputTypes.hidden,
														HtmlAttributes.name, WebForms.kOrderType,
														HtmlAttributes.value, MetaOrderType.buy.ToString());

										fm.Input(stream, HtmlAttributes.type, InputTypes.hidden,
														HtmlAttributes.name, WebForms.kSymbolPair,
														HtmlAttributes.value, CurrencyHelpers.GetMarketSymbolPair(CurrencyTypes.bitBTC, CurrencyTypes.BTC));

										using (new DivContainer(stream, HtmlAttributes.@class, "input-group"))
										{
											fm.Input(stream, HtmlAttributes.type, InputTypes.text,
																HtmlAttributes.name, WebForms.kReceivingAddress,
																HtmlAttributes.minlength, 1,
																HtmlAttributes.maxlength, 63,
																HtmlAttributes.required, true,
																HtmlAttributes.@class, "form-control",
																HtmlAttributes.placeholder, "Bitshares account name");

											using (new Span(stream, HtmlAttributes.@class, "input-group-btn"))
											{
												Button("Submit", HtmlAttributes.@class, "btn btn-info");
											}
										}
									}

									Alert("", "alert alert-danger", "bitsharesErrorId", true);
								}

								using (var fm = new FormContainer(stream))
								{
									using (new DivContainer(stream, HtmlAttributes.@class, "form-group has-success unhideBtsId"))
									{
										fm.Label(stream, "Your bitcoin deposit address");
										fm.Input(stream, HtmlAttributes.type, InputTypes.text,
															HtmlAttributes.id, "bitcoinDespositId",
															HtmlAttributes.@class, "form-control",
															HtmlAttributes.@readonly, "readonly",
															HtmlAttributes.style, "cursor:text;");
									}

									SPAN("Maximum {{market.ask_max | number:2}} BTC per transaction", "maxBtcId", "label label-info");
								}
							}
						}
						using (new DivContainer(stream, HtmlAttributes.@class, "col-sm-6"))
						{
							using (new Panel(stream, "Sell bitBTC", "panel panel-danger collapse", false, "sellId"))
							{
								P("Once you enter your bitcoin receiving address, we will generate your deposit address and send your bitcoins to you the instant we receive your bitBTC.");

								using (var fm = new FormContainer(stream, HtmlAttributes.method, "post",
																			HtmlAttributes.ajax, true,
																			HtmlAttributes.action, Routes.kSubmitAddress,
																			HtmlAttributes.handler, "OnSubmitAddressBtc"))
								{
									fm.Input(stream, HtmlAttributes.type, InputTypes.hidden,
														HtmlAttributes.name, WebForms.kOrderType,
														HtmlAttributes.value, MetaOrderType.sell.ToString());

									fm.Input(stream, HtmlAttributes.type, InputTypes.hidden,
														HtmlAttributes.id, "symbolPairId",
														HtmlAttributes.name, WebForms.kSymbolPair,
														HtmlAttributes.value, CurrencyHelpers.GetMarketSymbolPair(CurrencyTypes.bitBTC, CurrencyTypes.BTC));

									using (new DivContainer(stream, HtmlAttributes.@class, "form-group"))
									{
										fm.Label(stream, "Where shall we send your bitcoins?");
										using (new DivContainer(stream, HtmlAttributes.@class, "input-group"))
										{
											fm.Input(stream, HtmlAttributes.type, InputTypes.text,
																HtmlAttributes.name, WebForms.kReceivingAddress,
																HtmlAttributes.minlength, 25,
																HtmlAttributes.maxlength, 34,
																HtmlAttributes.required, true,
																HtmlAttributes.@class, "form-control",
																HtmlAttributes.placeholder, "Bitcoin address from your wallet");

											using (new Span(stream, HtmlAttributes.@class, "input-group-btn"))
											{
												Button("Submit", HtmlAttributes.@class, "btn btn-info");
											}
										}
									}

									Alert("", "alert alert-danger", "bitcoinErrorId", true);
								}

								using (var fm = new FormContainer(stream))
								{
									using (new DivContainer(stream, HtmlAttributes.@class, "unhideBtcId"))
									{
										using (new DivContainer(stream, HtmlAttributes.@class, "form-group has-success"))
										{
											fm.Label(stream, "Your bitshares deposit account");
											fm.Input(stream, HtmlAttributes.type, InputTypes.text,
																HtmlAttributes.id, "bitsharesDespositAccountId",
																HtmlAttributes.@class, "form-control",
																HtmlAttributes.@readonly, "readonly",
																HtmlAttributes.style, "cursor:text;");
										}

										using (new DivContainer(stream, HtmlAttributes.@class, "form-group has-success"))
										{
											fm.Label(stream, "Your bitshares deposit memo");
											fm.Input(stream, HtmlAttributes.type, InputTypes.text,
																HtmlAttributes.id, "bitsharesMemoId",
																HtmlAttributes.@class, "form-control",
																HtmlAttributes.@readonly, "readonly",
																HtmlAttributes.style, "cursor:text;");
										}
									}

									Button("Click to generate transaction", HtmlAttributes.@class, "btn btn-warning btn-xs pull-right unhideBtcId",
																			HtmlAttributes.onclick, "GenerateTransactionModal()");
									SPAN("Maximum {{market.bid_max | number:2}} bitBTC per transaction", "maxbitBtcId", "label label-info pull-left");
								}
							}
						}
					}
				}

				//
				// bullet points
				// 

				using (new DivContainer(stream, HtmlAttributes.@class, "container",
												HtmlAttributes.style, "margin-top:20px"))
				{
					using (new DivContainer(stream, HtmlAttributes.@class, "row"))
					{
						using (new DivContainer(stream, HtmlAttributes.@class, "col-sm-4"))
						{
							H4("<i class=\"glyphicon glyphicon-ok text-info\"></i>  No registration required");
							P("There is no need to register an account, just tell us where you'd like to receive the coins that you buy or sell.");
						}

						using (new DivContainer(stream, HtmlAttributes.@class, "col-sm-4"))
						{
							H4("<i class=\"glyphicon glyphicon-flash text-info\"></i>  Fast transactions");
							P("Only one confirmation is neccessary for buying or selling, which is around 7 minutes for a buy and around 3 seconds for a sell.");
						}

						using (new DivContainer(stream, HtmlAttributes.@class, "col-sm-4"))
						{
							H4("<i class=\"glyphicon glyphicon-lock text-info\"></i>  Safe");
							P("We don't hold any of our customer's funds, so there is nothing to get lost or stolen.");
						}
					}
				}

				using (new DivContainer(stream, HtmlAttributes.@class, "bg-primary hidden-xs",
												HtmlAttributes.style, "margin-top:20px"))
				{
					using (new DivContainer(stream, HtmlAttributes.@class, "container"))
					{
						using (new DivContainer(stream, HtmlAttributes.style, "margin:30px 0px 30px 0px"))
						{
							using (new DivContainer(stream, HtmlAttributes.@class, "row unhideBtsId unhideBtcId",
															HtmlAttributes.id, "myTransactionsId"))
							{
								using (new DivContainer(stream, HtmlAttributes.@class, "col-sm-12"))
								{
									H3("Your transactions");

									using (new Table(stream, "", 4, 4, "table noMargin", "Market", "Type", "Amount", "Date", "Status", "Notes"))
									{
										using (var tr = new TR(stream, "ng-repeat", "t in myTransactions"))
										{
											tr.TD("{{t.symbol_pair}}");
											tr.TD("{{t.order_type}}");
											tr.TD("{{t.amount}}");
											tr.TD("{{t.date*1000 | date:'MMM d, HH:mm'}}");
											tr.TD("{{t.status}}");
											tr.TD("{{t.notes}}");
										}
									}
								}
							}

							using (new DivContainer(stream, HtmlAttributes.@class, "unhideBtsId unhideBtcId"))
							{
								HR();
							}
							
							using (new DivContainer(stream, HtmlAttributes.@class, "row"))
							{
								using (new DivContainer(stream, HtmlAttributes.@class, "col-sm-12"))
								{
									H3("All transactions");

									using (new Table(stream, "", 4, 4, "table noMargin", "Market", "Type", "Amount", "Date"))
									{
										using (var tr = new TR(stream, "ng-repeat", "t in transactions"))
										{
											tr.TD("{{t.symbol_pair}}");
											tr.TD("{{t.order_type}}");
											tr.TD("{{t.amount}}");
											tr.TD("{{t.date*1000 | date:'MMM d, HH:mm'}}");
										}
									}
								}

								/*using (new DivContainer(stream, HtmlAttributes.@class, "col-sm-4"))
								{
									H3("All transactions");

									using (new Table(stream, "", 4, 4, "table noMargin", "Transaction"))
									{
										using (var tr = new TR(stream, "ng-repeat", "t in transactions"))
										{
											tr.TD("{{t.from}} -> {{t.to}}");
										}
									}
								}

								using (new DivContainer(stream, HtmlAttributes.@class, "col-sm-4"))
								{
									using (new Table(stream, "", 4, 4, "table noMargin", "Amount"))
									{
										using (var tr = new TR(stream, "ng-repeat", "t in transactions"))
										{
											tr.TD("{{t.amount}}");
										}
									}
								}

								using (new DivContainer(stream, HtmlAttributes.@class, "col-sm-4"))
								{
									using (new Table(stream, "", 4, 4, "table noMargin", "Date"))
									{
										using (var tr = new TR(stream, "ng-repeat", "t in transactions"))
										{
											tr.TD("{{t.date|date:'MMM d, HH:mm'}}");
										}
									}
								}*/
							}
						}
					}
				}

				Modal("Generate Bitshares transaction", "bitsharesModalId", () =>
				{
					using (var fm = new FormContainer(stream))
					{
						using (new DivContainer(stream, HtmlAttributes.@class, "form-group"))
						{
							fm.Label(stream, "Sending amount");

							using (new DivContainer(stream, HtmlAttributes.@class, "input-group"))
							{
								fm.Input(stream, HtmlAttributes.type, InputTypes.number,
												HtmlAttributes.name, "amount",
												HtmlAttributes.@class, "form-control",
												HtmlAttributes.required, true,
												HtmlAttributes.id, "gtxAmountId",
												HtmlAttributes.placeholder, "bitAsset quantity");

								SPAN("BTC", "", "input-group-addon");
							}
						}

						using (new DivContainer(stream, HtmlAttributes.@class, "form-group"))
						{
							fm.Label(stream, "Account name to send from");

							fm.Input(stream, HtmlAttributes.type, InputTypes.text,
											HtmlAttributes.name, "account",
											HtmlAttributes.@class, "form-control",
											HtmlAttributes.required, true,
											HtmlAttributes.id, "gtxAccountId",
											HtmlAttributes.placeholder, "Sending acount");
						}

						Href(stream, "",	HtmlAttributes.id, "bitsharesLinkId",
											HtmlAttributes.style, "display:none",
											HtmlAttributes.@class, "btn btn-success");
					}
				}, true, "", "modal", "close", false);
			}

			return null;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		protected override string GetPageSpecificJsFilename()
		{
			return "Pages/RequiredJs/Main.rs";
		}
	}
}
