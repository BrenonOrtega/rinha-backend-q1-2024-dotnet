use actix_web::{
    post,
    web::{Data, Json, Path},
    App, HttpResponse, HttpServer, Result,
};
use diesel_async::{
    pg::AsyncPgConnection,
    pooled_connection::{bb8::Pool, AsyncDieselConnectionManager},
    RunQueryDsl,
};
use dotenvy::dotenv;

use std::env;
use rinha_backend_q1_2024::{endpoints::post_transaction, models::AppState};



#[actix_web::main]
async fn main() -> std::io::Result<()> {
    dotenv().ok();

    let database_url = env::var("DATABASE_URL").expect("DATABASE_URL must be set");

    let http_port: String =
        env::var("API_PORT").expect("HTTP PORT TO BE BOUND SHOULD BE SET IN 'API_PORT'.");

    let http_port: u16 = http_port
        .parse()
        .expect("API_PORT should be a valid number.");

    let config =
        AsyncDieselConnectionManager::<diesel_async::AsyncPgConnection>::new(&database_url);
    let pool: Pool<AsyncPgConnection> = Pool::builder()
        .build(config)
        .await
        .expect("Error creating pool.");
    let app_state = AppState { pool };

    HttpServer::new(move || {
        let data: Data<AppState> = Data::new(app_state.clone());
        App::new().app_data(data).service(post_transaction)
    })
    .bind(("127.0.0.1", http_port))?
    .run()
    .await
}
